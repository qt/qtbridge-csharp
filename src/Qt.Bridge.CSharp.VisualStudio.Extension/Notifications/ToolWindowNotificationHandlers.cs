// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Notifications
{
    /// In-memory callbacks for a transient tool-window notification. Visual Studio supplies the
    /// click label, so this type only captures the callbacks for click, dismiss, and expire events.
    internal sealed record ToolWindowNotificationHandlers(
        Func<CancellationToken, Task>? OnClick = null,
        Func<CancellationToken, Task>? OnDismiss = null,
        Func<CancellationToken, Task>? OnExpire = null);
}
