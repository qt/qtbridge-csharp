// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.Notifications;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.WhatsNew
{
    internal sealed class WhatsNewLifecycleService
    {
        private const string OpenWhatsNewActionId = "qtbridge.whatsNew.open";
        private const string DismissWhatsNewActionId = "qtbridge.whatsNew.dismiss";

        private readonly SettingsStore settingsStore;
        private readonly IToastNotificationService toastNotifications;
        private readonly IToolWindowNotificationService? toolWindowNotifications;
        private readonly IExtensionLog log;
        private readonly object syncLock = new();
        private readonly NotificationAction openWhatsNewReminderAction;
        private readonly NotificationAction dismissWhatsNewReminderAction;
        private readonly IReadOnlyList<NotificationAction> whatsNewReminderActions;

        private string? currentVersion;
        private Guid? activeToolWindowNotificationId;

        public WhatsNewLifecycleService(
            SettingsStore stateStore,
            IToastNotificationService toastSvc,
            IToolWindowNotificationService? toolWindowNotificationsService,
            IExtensionLog extensionLog)
        {
            settingsStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            toastNotifications = toastSvc ?? throw new ArgumentNullException(nameof(toastSvc));
            toolWindowNotifications = toolWindowNotificationsService;
            log = extensionLog ?? throw new ArgumentNullException(nameof(extensionLog));

            whatsNewReminderActions = [
                openWhatsNewReminderAction = new NotificationAction(
                    "Open What's New",
                    OpenWhatsNewAsync,
                    OpenWhatsNewActionId),
                dismissWhatsNewReminderAction = new NotificationAction(
                    "Dismiss",
                    DismissWhatsNewReminderAsync,
                    DismissWhatsNewActionId)
            ];
        }

        public async Task InitializeAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            toolWindowNotifications?.RegisterDurableAction(openWhatsNewReminderAction);
            toolWindowNotifications?.RegisterDurableAction(dismissWhatsNewReminderAction);

            currentVersion = ExtensionVersionInfo.GetInstalledVersion();
            var lastSeenVersion = settingsStore.LastSeenVersion;
            var lastOpenedVersion = settingsStore.LastOpenedVersion;
            log.Verbose("Qt Bridge what's new lifecycle: "
                + $"currentVersion={currentVersion}, "
                + $"lastSeenVersion={lastSeenVersion ?? "<none>"}, "
                + $"lastOpenedVersion={lastOpenedVersion ?? "<none>"}.");

            if (string.Equals(lastSeenVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
                return;

            settingsStore.LastSeenVersion = currentVersion;

            if (string.IsNullOrWhiteSpace(lastSeenVersion)) {
                log.Info($"Qt Bridge What's New page first shown for version {currentVersion}.");
                await Commands.WhatsNew.OpenWhatsNewPageAsync(ct);
                return;
            }

            log.Info($"Qt Bridge update notification shown for version {currentVersion}.");
            await toastNotifications.ShowAsync(
                "Qt Bridge for C# was updated",
                $"Version {currentVersion} is installed.",
                ct,
                primary: openWhatsNewReminderAction,
                secondary: dismissWhatsNewReminderAction,
                onClosed: OnUpdateToastClosedAsync);
        }

        public async Task AcknowledgeCurrentVersionAsync(CancellationToken ct)
        {
            var version = currentVersion ?? ExtensionVersionInfo.GetInstalledVersion();
            settingsStore.LastOpenedVersion = version;

            Guid? notificationId;
            lock (syncLock) {
                notificationId = activeToolWindowNotificationId;
                activeToolWindowNotificationId = null;
            }

            if (notificationId is { } id && toolWindowNotifications != null)
                await toolWindowNotifications.DismissAsync(id, ct);
        }

        private Task OnUpdateToastClosedAsync(ToastCloseReason reason, CancellationToken ct)
        {
            return reason switch
            {
                ToastCloseReason.TimedOut => ShowToolWindowReminderAsync(ct),
                ToastCloseReason.Dismissed => LogToastDismissedAsync(),
                _ => Task.CompletedTask
            };
        }

        private async Task ShowToolWindowReminderAsync(CancellationToken ct)
        {
            if (toolWindowNotifications == null || string.IsNullOrWhiteSpace(currentVersion)) {
                log.Verbose("Qt Bridge what's new reminder skipped because the tool window "
                    + "notification service is not available.");
                return;
            }

            lock (syncLock) {
                if (activeToolWindowNotificationId != null)
                    return;
            }

            log.Info("Qt Bridge What's New reminder promoted to the Notifications tool "
                + $"window for version {currentVersion}.");
            var notificationId = await toolWindowNotifications.ShowAsync(
                "Qt Bridge for C# was updated",
                $"Version {currentVersion} is installed. Learn what changed.",
                whatsNewReminderActions,
                ct);

            if (notificationId is not { } id)
                return;

            lock (syncLock) {
                activeToolWindowNotificationId = id;
            }
        }

        private static Task OpenWhatsNewAsync(CancellationToken ct) =>
            Commands.WhatsNew.OpenWhatsNewPageAsync(ct);

        private Task DismissWhatsNewReminderAsync(CancellationToken ct)
        {
            lock (syncLock) {
                activeToolWindowNotificationId = null;
            }

            if (!string.IsNullOrWhiteSpace(currentVersion)) {
                log.Info("Qt Bridge What's New reminder dismissed from the Notifications tool "
                    + $"window for version {currentVersion}.");
            }
            return Task.CompletedTask;
        }

        private Task LogToastDismissedAsync()
        {
            if (!string.IsNullOrWhiteSpace(currentVersion))
                log.Info($"Qt Bridge What's New toast dismissed for version {currentVersion}.");
            return Task.CompletedTask;
        }
    }
}
