// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.WhatsNew
{
    internal sealed class WhatsNewLifecycleService(
        SettingsStore stateStore,
        IToastNotificationService toastNotifications,
        IExtensionLog log)
    {
        private readonly SettingsStore stateStore = stateStore
            ?? throw new ArgumentNullException(nameof(stateStore));
        private readonly IToastNotificationService toastNotifications = toastNotifications
            ?? throw new ArgumentNullException(nameof(toastNotifications));
        private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));

        public async Task InitializeAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var currentVersion = ExtensionVersionInfo.GetInstalledVersion();
            var lastSeenVersion = stateStore.LastSeenVersion;
            log.Verbose("Qt Bridge what's new lifecycle: "
                + $"currentVersion={currentVersion}, "
                + $"lastSeenVersion={lastSeenVersion ?? "<none>"}.");

            if (string.Equals(lastSeenVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
                return;

            stateStore.LastSeenVersion = currentVersion;

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
                primary: new NotificationAction(
                    "Open What's New",
                    Commands.WhatsNew.OpenWhatsNewPageAsync),
                secondary: new NotificationAction("Dismiss", _ => Task.CompletedTask));
        }
    }
}
