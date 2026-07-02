// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only
#if DEBUG

using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Notifications;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    [VisualStudioContribution]
    internal sealed class ShowNotification(IToastNotificationService toastSvc) : Command
    {
        private readonly IToastNotificationService toastNotifications = Requires.NotNull(toastSvc);

        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.ShowNotification.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.RunTestDialog,
                    IconSettings.IconAndText)
            };

        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken ct)
        {
#if WINDOWS_DESKTOP
            var providerClsidType = Type.GetTypeFromCLSID(
                new Guid(VsUserNotificationProvider.ProviderGuidString));
            if (providerClsidType != null)
                ServiceProvider.GlobalProvider.GetService(providerClsidType);

            var notifications = ExtensionPackage.Instance?.ToolWindowNotifications;
            if (notifications == null) {
                await Extensibility.Shell().ShowPromptAsync(
                    "The Qt Bridge in-proc notification bridge service is not available.",
                    PromptOptions.OK,
                    ct);
                return;
            }

            var notificationId = await notifications.ShowTransientAsync(
                "Qt Bridge for C#",
                "This notification validates VS Notifications toolwindow integration.",
                new ToolWindowNotificationHandlers(
                    OnClick: clickCt => toastNotifications.ShowAsync(
                        "Qt Bridge for C#",
                        "The Qt Bridge toolwindow action was clicked.",
                        clickCt),
                    OnDismiss: dismissCt => toastNotifications.ShowAsync(
                        "Qt Bridge for C#",
                        "The Qt Bridge toolwindow notification was dismissed.",
                        dismissCt)
                    ),
                ct);

            if (notificationId == null) {
                await Extensibility.Shell().ShowPromptAsync(
                    "Visual Studio did not accept the Qt Bridge notification. Check the Qt Bridge "
                    + "log for details.",
                    PromptOptions.OK,
                    ct);
            }
#endif
        }
    }
}
#endif
