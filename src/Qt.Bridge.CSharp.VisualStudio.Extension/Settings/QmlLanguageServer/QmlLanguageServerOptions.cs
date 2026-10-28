// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    /// <summary>
    /// Snapshot of the QML language server settings. A qmlls session keeps the snapshot it
    /// was started with.
    /// </summary>
    /// <param name="SemanticTokenDeltasEnabled">
    /// Passes the semantic token delta support and the token modifiers of qmlls to VS.
    /// </param>
    internal sealed record QmlLanguageServerOptions(
        bool SemanticTokenDeltasEnabled,
        LoggingOptions Logging)
    {
        internal static readonly QmlLanguageServerOptions Default = new(
            SemanticTokenDeltasEnabled: false,
            Logging: LoggingOptions.Default);
    }
}
