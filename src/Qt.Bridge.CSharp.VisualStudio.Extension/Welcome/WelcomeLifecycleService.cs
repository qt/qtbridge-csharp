// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Commands;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Welcome
{
    internal sealed class WelcomeLifecycleService(
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

            var currentVersion = GetInstalledVersion();
            var lastSeenVersion = stateStore.LastSeenVersion;
            log.Verbose("Qt Bridge lifecycle: "
                + $"currentVersion={currentVersion}, "
                + $"lastSeenVersion={lastSeenVersion ?? "<none>"}.");

            if (string.Equals(lastSeenVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
                return;

            stateStore.LastSeenVersion = currentVersion;

            if (string.IsNullOrWhiteSpace(lastSeenVersion)) {
                log.Info($"Qt Bridge welcome page first shown for version {currentVersion}.");
                await WhatsNew.OpenWelcomePageAsync(ct);
                return;
            }

            log.Info($"Qt Bridge update notification shown for version {currentVersion}.");
            await toastNotifications.ShowAsync(
                "Qt Bridge for C# was updated",
                $"Version {currentVersion} is installed.",
                ct,
                primary: new NotificationAction(
                    "Open Welcome",
                    WhatsNew.OpenWelcomePageAsync),
                secondary: new NotificationAction("Dismiss", _ => Task.CompletedTask));
        }

        private static string GetInstalledVersion()
        {
            var assemblyDir = Path.GetDirectoryName(typeof(ExtensionPackage).Assembly.Location)
                ?? string.Empty;
            var manifestPath = Path.Combine(assemblyDir, "extension.vsixmanifest");

            if (File.Exists(manifestPath)) {
                try {
                    var document = XDocument.Load(manifestPath);
                    var identity = document.Root?
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "Metadata")?
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "Identity");
                    var version = identity?.Attribute("Version")?.Value;
                    if (!string.IsNullOrWhiteSpace(version))
                        return version!;
                } catch {
                    // Fall back to the assembly version below.
                }
            }

            return typeof(ExtensionPackage).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        }
    }
}
