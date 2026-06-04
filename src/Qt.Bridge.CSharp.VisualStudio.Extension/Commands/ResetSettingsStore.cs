// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    [VisualStudioContribution]
    internal sealed class ResetSettingsStore(
        SettingsStore settingsStore,
        IToastNotificationService toastNotifications)
        : Command
    {
        private readonly SettingsStore settingsStore = Requires.NotNull(settingsStore);
        private readonly IToastNotificationService toastSrv = Requires.NotNull(toastNotifications);

        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.ResetSettingsStore.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.ClearCollection,
                    IconSettings.IconAndText)
            };

        public override Task ExecuteCommandAsync(IClientContext ctx, CancellationToken ct)
        {
            settingsStore.Reset();
            return toastSrv.ShowAsync("Qt Bridge for C#", "The settings store was reset.", ct);
        }
    }
}
