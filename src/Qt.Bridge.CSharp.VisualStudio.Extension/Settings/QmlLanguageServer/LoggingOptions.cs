// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    /// <summary>
    /// Snapshot of the QML language server logging settings read at qmlls launch time. Passed
    /// from <see cref="QmlLanguageServerProvider"/> to <see cref="QmlLanguageServerTransportPipe"/>
    /// so both can use the same user configuration for a given server session.
    /// </summary>
    internal sealed record LoggingOptions(
        bool QmllsLogEnabled,
        string QmllsLogFilePath,
        bool LspLogEnabled,
        string LspLogFilePath)
    {
        private const string QmllsLogFileName = "qtbridge-qmlls.log";
        private const string LspLogFileName = "qtbridge-lsp.log";

        internal static readonly LoggingOptions Default = new(
            QmllsLogEnabled: false,
            QmllsLogFilePath: "",
            LspLogEnabled: false,
            LspLogFilePath: "");

        internal static LoggingOptions Create(
            bool qmllsEnabled,
            string qmllsDirectory,
            bool lspEnabled,
            string lspDirectory)
        {
            return new LoggingOptions(
                QmllsLogEnabled: qmllsEnabled,
                qmllsEnabled ? ResolveFilePath(qmllsDirectory, QmllsLogFileName) : "",
                LspLogEnabled: lspEnabled,
                lspEnabled ? ResolveFilePath(lspDirectory, LspLogFileName) : "");
        }

        private static string ResolveFilePath(string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return "";
            var fullDirectory = Path.GetFullPath(directory);
            Directory.CreateDirectory(fullDirectory);
            return Path.Combine(fullDirectory, fileName);
        }
    }
}
