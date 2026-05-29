// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    internal interface INotificationService
    {
        Task ShowInfoAsync(string key, string message, CancellationToken ct);
        Task ShowWarningAsync(string key, string message, CancellationToken ct);
        Task ShowErrorAsync(string key, string message, CancellationToken ct);
        void ClearRateLimit(string key);
    }
}
