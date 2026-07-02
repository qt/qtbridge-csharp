// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Notifications
{
    /// <summary>
    /// Posts notifications to the Visual Studio Notifications tool window. Durable notifications
    /// are stored by Visual Studio and may still be clickable after restart, so their actions must
    /// be registered independently of the notification instance.
    /// </summary>
    internal interface IToolWindowNotificationService
    {
        /// <summary>
        /// Registers a durable action that may be referenced by <see cref="ShowAsync"/>.
        /// Register durable actions during startup so persisted notifications can invoke
        /// them even when the notification was created in an earlier Visual Studio session.
        /// </summary>
        void RegisterDurableAction(NotificationAction action);

        /// <summary>
        /// Shows a durable notification in the Visual Studio Notifications tool window.
        /// Every action must have a non-empty <see cref="NotificationAction.ActionId"/>
        /// and must already have been registered with <see cref="RegisterDurableAction"/>.
        /// </summary>
        Task<Guid?> ShowAsync(
            string title,
            string description,
            IReadOnlyList<NotificationAction>? actions,
            CancellationToken ct);

        /// <summary>
        /// Shows a transient notification backed by in-memory callbacks for the current session.
        /// Use this when the notification does not need to survive a Visual Studio restart.
        /// </summary>
        Task<Guid?> ShowTransientAsync(
            string title,
            string description,
            ToolWindowNotificationHandlers? handlers,
            CancellationToken ct);

        /// <summary>
        /// Dismisses a notification from the Visual Studio Notifications tool window.
        /// For transient notifications, this also removes the in-memory callback handlers
        /// associated with the notification id.
        /// </summary>
        Task DismissAsync(Guid notificationId, CancellationToken ct);
    }
}
