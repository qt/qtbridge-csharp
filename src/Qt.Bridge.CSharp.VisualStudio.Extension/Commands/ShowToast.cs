// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only
#if DEBUG

using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    /// <summary>
    /// Debug Extensions menu command for manually validating the Qt Bridge toast chain.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ShowToast(IToastNotificationService toastNotifications) : Command
    {
        private readonly IToastNotificationService toastNotifications =
            Requires.NotNull(toastNotifications);

        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.ShowToast.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.DebugInteractiveWindow,
                    IconSettings.IconAndText)
            };

        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken ct)
        {
            return toastNotifications.ShowAsync(
                "Qt Bridge for C#",
                "This notification validates the Qt Bridge toast rendering and action pipeline.",
                ct,
                primary: new NotificationAction(
                    "Show details",
                    cancellationToken => Extensibility.Shell().ShowPromptAsync(
                        "The debug toast action was invoked successfully.",
                        PromptOptions.OK,
                        cancellationToken)),
                secondary: new NotificationAction("Dismiss", _ => Task.CompletedTask));
        }
    }
}
#endif
