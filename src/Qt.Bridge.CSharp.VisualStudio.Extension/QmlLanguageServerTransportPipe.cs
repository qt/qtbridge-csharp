// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Channels;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    /// <summary>
    /// Wraps the QML Language Server process as an LSP transport pipe. Relays bytes between the
    /// VS LSP host and the process stdin/stdout. Accepts out-of-band notification injections via
    /// <see cref="EnqueueNotification"/>; notifications are held until after the LSP
    /// <c>initialized</c> handshake and then delivered without waiting for VS to send a message.
    /// </summary>
    internal sealed class QmlLanguageServerTransportPipe : IDuplexPipe, IDisposable
    {
        private readonly Process process;
        private readonly IExtensionLog log;
        private readonly Pipe vsReadPipe;
        private readonly Pipe vsWritePipe;
        private readonly CancellationTokenSource cts;
        private readonly Channel<byte[]> pendingNotifications =
            Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private readonly Task relayFromTask;
        private readonly Task relayToTask;

        public QmlLanguageServerTransportPipe(Process process, IExtensionLog extensionLog)
        {
            this.process = process;
            log = extensionLog;
            cts = new CancellationTokenSource();
            vsReadPipe = new Pipe();
            vsWritePipe = new Pipe();

            Input = vsReadPipe.Reader;
            Output = vsWritePipe.Writer;

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
            pendingNotifications.Writer.TryWrite(Encoding.UTF8.GetBytes(FrameLspMessage(json)));
        }

        public void Dispose()
        {
            process.Exited -= OnProcessExited;
            process.ErrorDataReceived -= OnErrorDataReceived;
            cts.Cancel();

            pendingNotifications.Writer.TryComplete();
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
            var logBuffer = new LspByteBuffer();
            Exception? fault = null;
            try {
                var source = process.StandardOutput.BaseStream;
                var writer = vsReadPipe.Writer;
                var buffer = new byte[4096];
                while (!ct.IsCancellationRequested) {
                    var read = await source.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read == 0)
                        break;
                    var mem = writer.GetMemory(read);
                    buffer.AsMemory(0, read).CopyTo(mem);
                    writer.Advance(read);
                    var result = await writer.FlushAsync(ct);
                    if (result.IsCompleted)
                        break;

                    // Parse incoming messages for diagnostic logging only.
                    logBuffer.Append(buffer.AsSpan(0, read));
                    while (logBuffer.TryExtractMessage(out var msg)) {
                        var method = LspByteBuffer.TryExtractMethod(msg);
                        log.Verbose(method != null
                            ? $"QML LS -> VS: {method} ({msg.Length} B)"
                            : $"QML LS -> VS: response ({msg.Length} B)");
                    }
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
            var initialized = false;
            Exception? fault = null;
            try {
                var reader = vsWritePipe.Reader;
                var dest = process.StandardInput.BaseStream;
                while (!ct.IsCancellationRequested) {
                    var vsReadTask = reader.ReadAsync(ct).AsTask();

                    // After initialized: drain pending notifications immediately without waiting
                    // for VS to send a message (project switches produce no VS LSP traffic).
                    if (initialized) {
                        while (!vsReadTask.IsCompleted) {
                            var task = pendingNotifications.Reader.WaitToReadAsync(ct).AsTask();
                            if (await Task.WhenAny(vsReadTask, task) == vsReadTask)
                                break;
                            while (pendingNotifications.Reader.TryRead(out var pending))
                                await dest.WriteAsync(pending, 0, pending.Length, ct);
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

                        await dest.WriteAsync(message, 0, message.Length, ct);

                        if (!initialized
                            && string.Equals(method, "initialized", StringComparison.Ordinal)) {
                            initialized = true;
                            // Drain anything enqueued before initialized arrived.
                            while (pendingNotifications.Reader.TryRead(out var pending))
                                await dest.WriteAsync(pending, 0, pending.Length, ct);
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

        /// <summary>
        /// Accumulates raw bytes from an LSP stream and extracts complete framed messages
        /// (Content-Length header + body) one at a time.
        /// </summary>
        private sealed class LspByteBuffer
        {
            private readonly List<byte> data = [];

            public void Append(ReadOnlySpan<byte> bytes)
            {
                data.AddRange(bytes.ToArray());
            }

            public bool TryExtractMessage(out byte[] message)
            {
                message = [];
                var headerEnd = FindHeaderEnd();
                if (headerEnd < 0)
                    return false;

                var headerText = Encoding.ASCII.GetString([..data.GetRange(0, headerEnd)]);
                var contentLength = ParseContentLength(headerText);
                if (contentLength < 0)
                    return false;

                var totalLength = headerEnd + 4 + contentLength;
                if (data.Count < totalLength)
                    return false;

                message = [..data.GetRange(0, totalLength)];
                data.RemoveRange(0, totalLength);
                return true;
            }

            /// <summary>
            /// Parses the JSON body of a framed LSP message and returns the <c>method</c> field,
            /// or <see langword="null"/> if the message has no method (e.g. a response) or if the
            /// body cannot be parsed.
            /// </summary>
            public static string? TryExtractMethod(byte[] message)
            {
                var text = Encoding.UTF8.GetString(message);
                var sep = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (sep < 0)
                    return null;

                var body = text.Substring(sep + 4);
                try {
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(body));
                    var serializer = new DataContractJsonSerializer(typeof(LspMethodDto));
                    return (serializer.ReadObject(ms) as LspMethodDto)?.Method;
                } catch (Exception) {
                    return null;
                }
            }

            private int FindHeaderEnd()
            {
                for (var i = 0; i <= data.Count - 4; i++) {
                    if (data[i] == '\r' && data[i + 1] == '\n'
                        && data[i + 2] == '\r' && data[i + 3] == '\n')
                        return i;
                }
                return -1;
            }

            private static int ParseContentLength(string headerText)
            {
                foreach (var line in headerText.Split('\n')) {
                    var trimmed = line.Trim('\r', ' ');
                    if (!trimmed.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var value = trimmed.Substring("Content-Length:".Length).Trim();
                    if (int.TryParse(value, out var length))
                        return length;
                }
                return -1;
            }
        }

        [DataContract]
        private sealed class LspMethodDto
        {
            [DataMember(Name = "method")]
            public string? Method { get; set; }
        }

        [DataContract]
        private sealed class WorkspaceFoldersNotificationDto
        {
            [DataMember(Name = "jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";

            [DataMember(Name = "method")]
            public string Method { get; set; } = "workspace/didChangeWorkspaceFolders";

            [DataMember(Name = "params")]
            public WorkspaceFoldersEventContainerDto? Params { get; set; }
        }

        [DataContract]
        private sealed class WorkspaceFoldersEventContainerDto
        {
            [DataMember(Name = "event")]
            public WorkspaceFoldersEventDto? Event { get; set; }
        }

        [DataContract]
        private sealed class WorkspaceFoldersEventDto
        {
            [DataMember(Name = "added")]
            public WorkspaceFolderDto[]? Added { get; set; }

            [DataMember(Name = "removed")]
            public WorkspaceFolderDto[]? Removed { get; set; }
        }

        [DataContract]
        private sealed class WorkspaceFolderDto
        {
            [DataMember(Name = "uri")]
            public string? Uri { get; set; }

            [DataMember(Name = "name")]
            public string? Name { get; set; }
        }

        [DataContract]
        private sealed class AddBuildDirsNotificationDto
        {
            [DataMember(Name = "jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";

            [DataMember(Name = "method")]
            public string Method { get; set; } = "$/addBuildDirs";

            [DataMember(Name = "params")]
            public AddBuildDirsParamsDto? Params { get; set; }
        }

        [DataContract]
        private sealed class AddBuildDirsParamsDto
        {
            [DataMember(Name = "buildDirsToSet")]
            public BuildDirsEntryDto[]? BuildDirsToSet { get; set; }
        }

        [DataContract]
        private sealed class BuildDirsEntryDto
        {
            [DataMember(Name = "baseUri")]
            public string? BaseUri { get; set; }

            [DataMember(Name = "buildDirs")]
            public string[]? BuildDirs { get; set; }
        }
    }
}
