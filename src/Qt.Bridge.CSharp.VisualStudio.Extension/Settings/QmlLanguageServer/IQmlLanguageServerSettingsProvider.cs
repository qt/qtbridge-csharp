// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal interface IQmlLanguageServerSettingsProvider
    {
        /// <summary>
        /// Raised when a setting may have changed. Can also be raised without a change, so
        /// subscribers compare the options themselves.
        /// </summary>
        event Action? Changed;

        Task<QmlLanguageServerOptions> GetOptionsAsync(CancellationToken ct);
    }
}
