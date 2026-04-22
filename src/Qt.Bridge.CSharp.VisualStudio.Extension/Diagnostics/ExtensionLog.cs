// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Documents;

#pragma warning disable VSEXTPREVIEW_OUTPUTWINDOW

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    /// <summary>
    /// Writes extension diagnostics to <see cref="TraceSource"/> and to the Qt Bridge Visual
    /// Studio output channel.
    /// </summary>
    internal sealed class ExtensionLog : IExtensionLog
    {
        private readonly TraceSource traceSource;
        private readonly VisualStudioExtensibility extensibility;
        private readonly object channelLock = new();
        private Task<OutputChannel?>? channelTask;

        public ExtensionLog(TraceSource source, VisualStudioExtensibility vsExt)
        {
            traceSource = source ?? throw new ArgumentNullException(nameof(source));
            extensibility = vsExt ?? throw new ArgumentNullException(nameof(vsExt));
            traceSource.Listeners.Add(new DefaultTraceListener());
            traceSource.Switch.Level = SourceLevels.Verbose;
        }

        public void Verbose(string message)
        {
            traceSource.TraceEvent(TraceEventType.Verbose, 0, message);
        }

        public void Info(string message)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, message);
            WriteToPane($"[Info] {message}");
        }

        public void Warning(string message)
        {
            traceSource.TraceEvent(TraceEventType.Warning, 0, message);
            WriteToPane($"[Warning] {message}");
        }

        public void Error(string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message}{Environment.NewLine}{exception}"
                : message;
            traceSource.TraceEvent(TraceEventType.Error, 0, fullMessage);
            WriteToPane($"[Error] {fullMessage}");
        }

        private void WriteToPane(string message)
        {
            _ = WriteToPaneAsync(message);
        }

        private async Task WriteToPaneAsync(string message)
        {
            try {
                var channel = await GetOrCreateChannelAsync();
                if (channel != null)
                    await channel.WriteLineAsync($"[Qt Bridge] {message}");
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                traceSource.TraceEvent(TraceEventType.Warning, 0,
                    $"Qt Bridge: failed to write to output pane: {ex.Message}");
            }
        }

        private Task<OutputChannel?> GetOrCreateChannelAsync()
        {
            lock (channelLock) {
                return channelTask ??= CreateChannelAsync();
            }
        }

        private async Task<OutputChannel?> CreateChannelAsync()
        {
            try {
                return await extensibility.Views().Output
                    .CreateOutputChannelAsync("Qt Bridge for C#", CancellationToken.None);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                traceSource.TraceEvent(TraceEventType.Warning, 0,
                    $"Qt Bridge: failed to create output channel: {ex.Message}");
                return null;
            }
        }
    }
}

#pragma warning restore VSEXTPREVIEW_OUTPUTWINDOW
