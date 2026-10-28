// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    /// <summary>
    /// QML language server settings, read when qmlls starts.
    /// </summary>
    internal sealed record QmlLanguageServerOptions(LoggingOptions Logging)
    {
        internal static readonly QmlLanguageServerOptions Default = new(
            Logging: LoggingOptions.Default);
    }
}
