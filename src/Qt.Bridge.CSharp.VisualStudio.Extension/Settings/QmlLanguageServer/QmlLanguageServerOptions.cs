// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    /// <summary>
    /// QML language server settings, read when qmlls starts.
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
