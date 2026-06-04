// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Channels;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer.Contracts;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    /// <summary>
    /// Wraps the QML Language Server process as an LSP transport pipe. Relays bytes between the
    /// VS LSP host and the process stdin/stdout. Accepts out-of-band notification injections via
    /// <see cref="EnqueueNotification"/>; notifications are held until after the LSP
    /// <c>initialized</c> handshake and then delivered without waiting for VS to send a message.
    /// </summary>
    internal sealed partial class QmlLanguageServerTransportPipe : IDuplexPipe, IDisposable
    {
        private readonly Process process;
        private readonly IExtensionLog log;
        private readonly Pipe vsReadPipe;
        private readonly Pipe vsWritePipe;
        private readonly CancellationTokenSource cts;
        private readonly TaskCompletionSource<bool> serverInitializedSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Channel<byte[]> pendingNotifications =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private readonly Channel<byte[]> pendingServerRequests =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private readonly object injectedRequestLock = new();
        private readonly HashSet<string> injectedRequestIds = [];
        private long nextInjectedRequestId;
        private readonly Task relayFromTask;
        private readonly Task relayToTask;

        public QmlLanguageServerTransportPipe(
            Process process,
            IExtensionLog extensionLog,
            LoggingOptions loggingOptions)
        {
            this.process = process;
            log = extensionLog;
            cts = new CancellationTokenSource();
            vsReadPipe = new Pipe();
            vsWritePipe = new Pipe();

            Input = vsReadPipe.Reader;
            Output = vsWritePipe.Writer;

            InitializeLogging(loggingOptions);

            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;
            process.ErrorDataReceived += OnErrorDataReceived;
            process.BeginErrorReadLine();

            var ct = cts.Token;
            relayFromTask = RelayFromProcessAsync(ct);
            relayToTask = RelayToProcessAsync(ct);
        }

        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        /// <summary>
        /// Enqueues <paramref name="json"/> (a raw JSON notification body) for delivery to the QML
        /// Language Server. Framing is applied automatically. The notification is sent only after
        /// the LSP <c>initialized</c> handshake has completed.
        /// </summary>
        public void EnqueueNotification(string json)
        {
            var framed = Encoding.UTF8.GetBytes(FrameLspMessage(json));
            TraceLspTraffic("EXT -> QML LS (queued)", framed);
            pendingNotifications.Writer.TryWrite(framed);
        }

        /// <summary>
        /// Requests that Visual Studio refresh semantic tokens for open documents. This mimics
        /// servers such as rust-analyzer that explicitly ask the client to refresh after startup.
        /// </summary>
        public void EnqueueSemanticTokensRefresh()
        {
            var requestId = "qtbridge-semanticTokens-refresh-"
                + Interlocked.Increment(ref nextInjectedRequestId);
            lock (injectedRequestLock)
                injectedRequestIds.Add(requestId);

            var framed = Encoding.UTF8.GetBytes(FrameLspMessage(
                BuildSemanticTokensRefreshRequest(requestId)));
            pendingServerRequests.Writer.TryWrite(framed);
        }

        public void Dispose()
        {
            process.Exited -= OnProcessExited;
            process.ErrorDataReceived -= OnErrorDataReceived;
            cts.Cancel();

            pendingNotifications.Writer.TryComplete();
            pendingServerRequests.Writer.TryComplete();

            try {
                // Use JoinableTaskFactory.Run so VS main-thread pumping continues while we
                // wait, avoiding the deadlock risk of a raw Task.Wait() on a VS-managed thread.
#pragma warning disable VSTHRD003
                _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(
                    () => Task.WhenAny(
                        Task.WhenAll(relayFromTask, relayToTask),
                        Task.Delay(TimeSpan.FromMilliseconds(500))));
#pragma warning restore VSTHRD003
            } catch (Exception) {}

            try {
                if (!process.HasExited)
                    process.Kill();
            } catch (Exception) {}

            process.Dispose();
            cts.Dispose();
        }

        /// <summary>
        /// Builds the JSON body for a <c>workspace/didChangeWorkspaceFolders</c> notification.
        /// <paramref name="folderUri"/> must be a file URI. Pass <paramref name="add"/>
        /// <see langword="true"/> to register, <see langword="false"/> to unregister.
        /// </summary>
        internal static string BuildWorkspaceFolderNotification(string folderUri, bool add)
        {
            var folder = new WorkspaceFolderDto {
                Uri = folderUri,
                Name = GetFolderName(folderUri)
            };
            var dto = new WorkspaceFoldersNotificationDto {
                Params = new WorkspaceFoldersEventContainerDto {
                    Event = new WorkspaceFoldersEventDto {
                        Added = add ? [folder] : [],
                        Removed = add ? [] : [folder]
                    }
                }
            };
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(WorkspaceFoldersNotificationDto))
                .WriteObject(ms, dto);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>
        /// Builds the JSON body for a <c>$/addBuildDirs</c> notification.
        /// <paramref name="folderUri"/> must be a file URI identifying the project source root;
        /// <paramref name="buildDirs"/> are filesystem paths passed directly to qmlls.
        /// </summary>
        internal static string BuildAddBuildDirsNotification(
            string folderUri, IEnumerable<string> buildDirs)
        {
            var dto = new AddBuildDirsNotificationDto {
                Params = new AddBuildDirsParamsDto {
                    BuildDirsToSet = [
                        new BuildDirsEntryDto {
                            BaseUri = folderUri,
                            BuildDirs = [..buildDirs]
                        }
                    ]
                }
            };
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(AddBuildDirsNotificationDto))
                .WriteObject(ms, dto);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string FrameLspMessage(string body) =>
            $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";

        private static string BuildSemanticTokensRefreshRequest(string requestId)
        {
            var dto = new SemanticTokensRefreshRequestDto {
                Id = requestId
            };
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(SemanticTokensRefreshRequestDto))
                .WriteObject(ms, dto);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetFolderName(string folderUri)
        {
            try {
                var localPath = new Uri(folderUri).LocalPath.TrimEnd('/', '\\');
                var name = Path.GetFileName(localPath);
                return string.IsNullOrEmpty(name) ? folderUri : name;
            } catch {
                return folderUri;
            }
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            log.Info("QML Language Server: process exited.");
            // Signal the relay tasks to stop. They will complete the pipe ends
            // as part of their own cleanup, keeping completion single-ownership.
            try {
                cts.Cancel();
            } catch (ObjectDisposedException) {
                // Dispose() beat us here - nothing to do.
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                log.Info($"QML Language Server (stderr): {e.Data}");
        }

        private async Task RelayFromProcessAsync(CancellationToken ct)
        {
            log.Info("QML Language Server transport: process -> VS relay started.");
            var logBuffer = lspLogEnabled ? new LspByteBuffer() : null;
            Exception? fault = null;
            try {
                var source = process.StandardOutput.BaseStream;
                var writer = vsReadPipe.Writer;
                var buffer = new byte[4096];
                var readTask = source.ReadAsync(buffer, 0, buffer.Length, ct);
                while (!ct.IsCancellationRequested) {
                    if (!serverInitializedSource.Task.IsCompleted) {
#pragma warning disable VSTHRD003 // relay task owns both awaited tasks
                        var completed = await Task.WhenAny(readTask, serverInitializedSource.Task);
#pragma warning restore VSTHRD003
                        if (completed != readTask)
                            continue;
                    } else {
                        var pendingTask = pendingServerRequests.Reader.WaitToReadAsync(ct).AsTask();
                        var completed = await Task.WhenAny(readTask, pendingTask);
                        if (completed != readTask) {
                            while (pendingServerRequests.Reader.TryRead(out var pending)) {
                                var pendingMem = writer.GetMemory(pending.Length);
                                pending.CopyTo(pendingMem);
                                writer.Advance(pending.Length);
                            }
                            var flush = await writer.FlushAsync(ct);
                            if (flush.IsCompleted)
                                break;
                            continue;
                        }
                    }

                    var read = await readTask;
                    if (read == 0)
                        break;
                    var mem = writer.GetMemory(read);
                    buffer.AsMemory(0, read).CopyTo(mem);
                    writer.Advance(read);
                    var result = await writer.FlushAsync(ct);
                    if (result.IsCompleted)
                        break;

                    // Parse incoming messages for optional LSP file logging only.
                    if (logBuffer != null) {
                        logBuffer.Append(buffer.AsSpan(0, read));
                        while (logBuffer.TryExtractMessage(out var msg)) {
                            var method = LspByteBuffer.TryExtractMethod(msg);
                            log.Verbose(method != null
                                ? $"QML LS -> VS: {method} ({msg.Length} B)"
                                : $"QML LS -> VS: response ({msg.Length} B)");
                            TraceLspTraffic("QML LS -> VS", msg, method);
                        }
                    }

                    readTask = source.ReadAsync(buffer, 0, buffer.Length, ct);
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                fault = ex;
            }
            await vsReadPipe.Writer.CompleteAsync(fault);
            if (fault != null)
                log.Error("QML Language Server transport: process -> VS relay faulted.", fault);
            else
                log.Info("QML Language Server transport: process -> VS relay completed.");
        }

        private async Task RelayToProcessAsync(CancellationToken ct)
        {
            log.Info("QML Language Server transport: VS -> process relay started.");
            var msgBuffer = new LspByteBuffer();
            var handshakeComplete = false;
            Exception? fault = null;
            try {
                var reader = vsWritePipe.Reader;
                var dest = process.StandardInput.BaseStream;
                while (!ct.IsCancellationRequested) {
                    var vsReadTask = reader.ReadAsync(ct).AsTask();

                    // After initialized: drain pending notifications immediately without waiting
                    // for VS to send a message (project switches produce no VS LSP traffic).
                    if (handshakeComplete) {
                        while (!vsReadTask.IsCompleted) {
                            var task = pendingNotifications.Reader.WaitToReadAsync(ct).AsTask();
                            if (await Task.WhenAny(vsReadTask, task) == vsReadTask)
                                break;
                            while (pendingNotifications.Reader.TryRead(out var pending)) {
                                TraceLspTraffic("EXT -> QML LS (written)", pending);
                                await dest.WriteAsync(pending, 0, pending.Length, ct);
                            }
                            await dest.FlushAsync(ct);
                        }
                    }

                    var result = await vsReadTask;
                    var sequence = result.Buffer;
                    foreach (var segment in sequence)
                        msgBuffer.Append(segment.Span);
                    reader.AdvanceTo(sequence.End);

                    while (msgBuffer.TryExtractMessage(out var message)) {
                        var method = LspByteBuffer.TryExtractMethod(message);
                        log.Verbose(method != null
                            ? $"VS -> QML LS: {method} ({message.Length} B)"
                            : $"VS -> QML LS: response ({message.Length} B)");
                        TraceLspTraffic("VS -> QML LS", message, method);

                        if (IsInjectedRequestResponse(message)
                            || IsVsRefreshEchoNotification(message, method)) {
                            continue;
                        }

                        await dest.WriteAsync(message, 0, message.Length, ct);

                        if (!handshakeComplete
                            && string.Equals(method, "initialized", StringComparison.Ordinal)) {
                            handshakeComplete = true;
                            serverInitializedSource.TrySetResult(true);
                            // Drain anything enqueued before initialized arrived.
                            while (pendingNotifications.Reader.TryRead(out var pending)) {
                                TraceLspTraffic("EXT -> QML LS (written)", pending);
                                await dest.WriteAsync(pending, 0, pending.Length, ct);
                            }
                        }

                        await dest.FlushAsync(ct);
                    }

                    if (result.IsCompleted)
                        break;
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                fault = ex;
            }
            await vsWritePipe.Reader.CompleteAsync(fault);
            if (fault != null)
                log.Error("QML Language Server transport: VS -> process relay faulted.", fault);
            else
                log.Info("QML Language Server transport: VS -> process relay completed.");
        }

        private bool IsInjectedRequestResponse(byte[] message)
        {
            var body = LspByteBuffer.TryExtractBody(message);
            if (body == null || LspByteBuffer.TryExtractMethod(message) != null)
                return false;

            lock (injectedRequestLock) {
                foreach (var id in injectedRequestIds) {
                    if (body.IndexOf($"\"id\":\"{id}\"", StringComparison.Ordinal) < 0)
                        continue;
                    injectedRequestIds.Remove(id);
                    return true;
                }
            }
            return false;
        }

        private static bool IsVsRefreshEchoNotification(byte[] message, string? method)
        {
            if (!string.Equals(method, "NotificationReceived", StringComparison.Ordinal))
                return false;
            var body = LspByteBuffer.TryExtractBody(message);
            return body?.IndexOf(
                "\"MethodName\":\"workspace/semanticTokens/refresh\"",
                StringComparison.Ordinal) >= 0;
        }

    }
}
