// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal interface ILoggingSettingsProvider
    {
        Task<LoggingOptions> GetOptionsAsync(CancellationToken ct);
    }
}
