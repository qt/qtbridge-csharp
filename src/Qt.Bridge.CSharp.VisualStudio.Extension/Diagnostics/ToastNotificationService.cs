// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Shell;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    internal sealed class ToastNotificationService(
        INotificationService notifications,
        IExtensionLog log)
        : IToastNotificationService
    {
        private static readonly TimeSpan ToastDuration = TimeSpan.FromSeconds(5);
        private readonly INotificationService notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));
        private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));

        public async Task ShowAsync(
            string title,
            string detail,
            CancellationToken ct,
            NotificationAction? primary = null,
            NotificationAction? secondary = null)
        {
            if (await TryShowToastAsync(title, detail, ct, primary, secondary))
                return;

            var message = string.IsNullOrEmpty(detail) ? title : $"{title}  {detail}";
            var actions = new[] { primary, secondary }.OfType<NotificationAction>().ToArray();
            await notifications.ShowInfoAsync(title, message, actions, ct);
        }

        private async Task<bool> TryShowToastAsync(
            string title,
            string detail,
            CancellationToken ct,
            NotificationAction? primary,
            NotificationAction? secondary)
        {
#if WINDOWS_DESKTOP
            try {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                return await ToastWindow.TryShowAsync(title, detail, primary, secondary,
                    ToastDuration, log, ct);
            } catch (Exception ex) {
                log.Warning($"Toast notification failed: {ex.Message}");
                return false;
            }
#else
            await Task.CompletedTask;
            return false;
#endif
        }
    }
}
