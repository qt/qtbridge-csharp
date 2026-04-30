// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerTransportPipe
    {
        private static readonly ConcurrentDictionary<string, object> LspLogPathLocks =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, bool> LspLogPathsReset =
            new(StringComparer.OrdinalIgnoreCase);

        private bool lspLogEnabled;
        private string lspLogFilePath = string.Empty;

        private void InitializeLogging(LoggingOptions loggingOptions)
        {
            lspLogEnabled = loggingOptions.LspLogEnabled
                && !string.IsNullOrWhiteSpace(loggingOptions.LspLogFilePath);
            lspLogFilePath = loggingOptions.LspLogFilePath;
            if (lspLogEnabled)
                ResetLspLogOnce(lspLogFilePath);
        }

        /// <summary>
        /// The LSP method name already extracted by the caller, or <see langword="null"/> when
        /// not yet known (the method will then be re-extracted from <paramref name="message"/>).
        /// Pass the pre-extracted value wherever it is already available to avoid a second
        /// JSON parse of the same bytes.
        /// </summary>
        private void TraceLspTraffic(string direction, byte[] message, string? method = null)
        {
            if (!lspLogEnabled)
                return;

            try {
                method ??= LspByteBuffer.TryExtractMethod(message) ?? "response";
                var body = LspByteBuffer.TryExtractBody(message) ?? "<unparseable>";
                var entry = string.Join(Environment.NewLine,
                    $"[{DateTimeOffset.Now:O}] {direction}: {method} ({message.Length} B)",
                    body, string.Empty);

                lock (GetLspLogLock(lspLogFilePath))
                    File.AppendAllText(lspLogFilePath, entry + Environment.NewLine, Encoding.UTF8);
            } catch (Exception) {}
        }

        private static object GetLspLogLock(string path) =>
            LspLogPathLocks.GetOrAdd(path, _ => new object());

        private static void ResetLspLogOnce(string path)
        {
            if (!LspLogPathsReset.TryAdd(path, true))
                return;
            try {
                lock (GetLspLogLock(path))
                    File.WriteAllText(path, string.Empty, Encoding.UTF8);
            } catch (Exception) {}
        }
    }
}
