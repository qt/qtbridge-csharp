// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    internal interface IToastNotificationService
    {
        Task ShowAsync(
            string title,
            string detail,
            CancellationToken ct,
            NotificationAction? primary = null,
            NotificationAction? secondary = null);
    }
}
