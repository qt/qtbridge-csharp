// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    /// <summary>
    /// Wraps the QML Language Server process as an LSP transport pipe. Relays bytes between the VS
    /// LSP host and the process stdin/stdout.
    /// </summary>
    internal sealed class QmlLanguageServerTransportPipe : IDuplexPipe, IDisposable
    {
        private readonly Process process;
        private readonly TraceSource logger;
        private readonly Pipe vsReadPipe;
        private readonly Pipe vsWritePipe;
        private readonly CancellationTokenSource cts;
        private readonly string? addBuildDirsNotification;
        private readonly Task relayFromTask;
        private readonly Task relayToTask;

        /// <summary>
        /// Constructs the transport pipe, starts background relay tasks, and sets up process event
        /// handlers.
        /// </summary>

        /// <summary>
        /// Wraps the QML Language Server process as an LSP transport pipe.
        /// <para>
        /// When <paramref name="projectSourceDir"/> is non-null and valid, intercepts the
        /// <c>initialized</c> notification sent by VS and injects a <c>$/addBuildDirs</c>
        /// notification immediately after it, mapping the user project root to the Qt-native
        /// build directories so the QML Language Server covers user-authored QML files in
        /// addition to the generated source tree.
        /// </para>
        /// </summary>
        public QmlLanguageServerTransportPipe(
            Process process,
            TraceSource logger,
            string? projectSourceDir,
            IReadOnlyCollection<string> buildDirs)
        {
            this.process = process;
            this.logger = logger;
            cts = new CancellationTokenSource();
            vsReadPipe = new Pipe();
            vsWritePipe = new Pipe();

            var addBuildDirsBaseDir = GetAddBuildDirsBaseDirectory(projectSourceDir);
            if (addBuildDirsBaseDir != null && buildDirs.Count > 0) {
                addBuildDirsNotification = BuildNotification(addBuildDirsBaseDir, buildDirs);
                logger.TraceEvent(TraceEventType.Information, 0,
                    "QML Language Server: will inject $/addBuildDirs"
                    + $" (baseUri={new Uri(addBuildDirsBaseDir).AbsoluteUri},"
                    + $" {buildDirs.Count} build dir(s)).");
            } else {
                logger.TraceEvent(TraceEventType.Information, 0,
                    "QML Language Server: $/addBuildDirs injection disabled"
                    + " (no valid projectSourceDir or no build dirs).");
            }

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

        public void Dispose()
        {
            process.Exited -= OnProcessExited;
            process.ErrorDataReceived -= OnErrorDataReceived;
            cts.Cancel();

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

        private void OnProcessExited(object? sender, EventArgs e)
        {
            logger.TraceEvent(TraceEventType.Information, 0,
                "QML Language Server: process exited.");
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
            if (!string.IsNullOrEmpty(e.Data)) {
                logger.TraceEvent(TraceEventType.Information, 0,
                    $"QML Language Server (stderr): {e.Data}");
            }
        }

        private async Task RelayFromProcessAsync(CancellationToken ct)
        {
            logger.TraceEvent(TraceEventType.Information, 0,
                "QML Language Server transport: process -> VS relay started.");
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
                        logger.TraceEvent(TraceEventType.Verbose, 0,
                            method != null
                                ? $"QML LS -> VS: {method} ({msg.Length} B)"
                                : $"QML LS -> VS: response ({msg.Length} B)");
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                fault = ex;
            }
            await vsReadPipe.Writer.CompleteAsync(fault);
            if (fault != null) {
                logger.TraceEvent(TraceEventType.Error, 0,
                    $"QML Language Server transport: process -> VS relay faulted: {fault.Message}");
            } else {
                logger.TraceEvent(TraceEventType.Information, 0,
                    "QML Language Server transport: process -> VS relay completed.");
            }
        }

        private async Task RelayToProcessAsync(CancellationToken ct)
        {
            logger.TraceEvent(TraceEventType.Information, 0,
                "QML Language Server transport: VS -> process relay started.");
            var msgBuffer = new LspByteBuffer();
            var notificationSent = addBuildDirsNotification == null;
            Exception? fault = null;
            try {
                var reader = vsWritePipe.Reader;
                var dest = process.StandardInput.BaseStream;
                while (!ct.IsCancellationRequested) {
                    var result = await reader.ReadAsync(ct);
                    var sequence = result.Buffer;

                    foreach (var segment in sequence)
                        msgBuffer.Append(segment.Span);
                    reader.AdvanceTo(sequence.End);

                    while (msgBuffer.TryExtractMessage(out var message)) {
                        var method = LspByteBuffer.TryExtractMethod(message);
                        logger.TraceEvent(TraceEventType.Verbose, 0,
                            method != null
                                ? $"VS -> QML LS: {method} ({message.Length} B)"
                                : $"VS -> QML LS: response ({message.Length} B)");

                        await dest.WriteAsync(message, 0, message.Length, ct);

                        if (!notificationSent
                            && string.Equals(method, "initialized", StringComparison.Ordinal)) {
                            logger.TraceEvent(TraceEventType.Information, 0,
                                "QML Language Server: 'initialized' received,"
                                + " injecting $/addBuildDirs.");
                            var notif = Encoding.UTF8.GetBytes(addBuildDirsNotification!);
                            await dest.WriteAsync(notif, 0, notif.Length, ct);
                            notificationSent = true;
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
            if (fault != null) {
                logger.TraceEvent(TraceEventType.Error, 0,
                    $"QML Language Server transport: VS -> process relay faulted: {fault.Message}");
            } else {
                logger.TraceEvent(TraceEventType.Information, 0,
                    "QML Language Server transport: VS -> process relay completed.");
            }
        }

        /// <summary>
        /// Builds a framed LSP notification with the correct QML Language Server $/addBuildDirs
        /// shape:
        /// <code>
        /// { "jsonrpc":"2.0", "method":"$/addBuildDirs",
        ///   "params": { "buildDirsToSet": [{ "baseUri": "&lt;uri&gt;",
        ///                                    "buildDirs": ["&lt;path&gt;", ...] }] } }
        /// </code>
        /// <paramref name="projectSourceDir"/> is converted to a file URI for baseUri.
        /// <paramref name="buildDirs"/> are passed as raw filesystem paths. The QML Language
        /// Server receives them with QString::fromUtf8 and passes them to loadSettingsFrom.
        /// </summary>
        private static string BuildNotification(
            string projectSourceDir, IEnumerable<string> buildDirs)
        {
            var dto = new AddBuildDirsNotificationDto
            {
                Params = new AddBuildDirsParamsDto
                {
                    BuildDirsToSet =
                    [
                        new BuildDirsEntryDto
                        {
                            BaseUri = new Uri(projectSourceDir).AbsoluteUri,
                            BuildDirs = [..buildDirs]
                        }
                    ]
                }
            };

            using var ms = new MemoryStream();
            var serializer = new DataContractJsonSerializer(
                typeof(AddBuildDirsNotificationDto));
            serializer.WriteObject(ms, dto);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            return $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";
        }

        private static string? GetAddBuildDirsBaseDirectory(string? projectSourceDir)
        {
            if (string.IsNullOrWhiteSpace(projectSourceDir))
                return null;

            try {
                var fullPath = Path.GetFullPath(projectSourceDir);
                return Directory.Exists(fullPath) ? fullPath : null;
            } catch (Exception ex) when (ex is ArgumentException
                or IOException
                or NotSupportedException
                or UnauthorizedAccessException) {
                return null;
            }
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
