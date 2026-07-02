// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Notifications
{
    /// <summary>
    /// User-facing action metadata plus the callback to execute when the action is invoked.
    /// For durable tool-window notifications, <see cref="ActionId"/> identifies the action
    /// across Visual Studio sessions.
    /// </summary>
    internal sealed record NotificationAction(
        string Text,
        Func<CancellationToken, Task> ExecuteAsync,
        string? ActionId = null);
}
