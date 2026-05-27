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
    internal sealed class ExtensionLog(TraceSource source, VisualStudioExtensibility vsExt)
        : IExtensionLog
    {
        private readonly TraceExtensionLog traceLog = new(source);
        private readonly VisualStudioExtensibility extensibility = vsExt
            ?? throw new ArgumentNullException(nameof(vsExt));
        private readonly object channelLock = new();
        private Task<OutputChannel?>? channelTask;

        public void Verbose(string message)
        {
            traceLog.Verbose(message);
        }

        public void Info(string message)
        {
            traceLog.Info(message);
            WriteToPane($"[Info] {message}");
        }

        public void Warning(string message)
        {
            traceLog.Warning(message);
            WriteToPane($"[Warning] {message}");
        }

        public void Error(string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message}{Environment.NewLine}{exception}"
                : message;
            traceLog.Error(message, exception);
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
                traceLog.Warning($"Qt Bridge: failed to write to output pane: {ex.Message}");
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
                traceLog.Warning($"Qt Bridge: failed to create output channel: {ex.Message}");
                return null;
            }
        }
    }
}

#pragma warning restore VSEXTPREVIEW_OUTPUTWINDOW
