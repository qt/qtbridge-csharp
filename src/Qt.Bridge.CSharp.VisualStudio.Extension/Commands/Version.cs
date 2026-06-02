// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    [VisualStudioContribution]
    internal sealed class Version : Command
    {
        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.Version.DisplayName%")
            {
                // Use an unsatisfiable capability to keep the command permanently
                // disabled. It acts then as a read-only version label in the menu.
                EnabledWhen = ActivationConstraint.ActiveProjectCapability("_VersionLabel_")
            };

        public override Task ExecuteCommandAsync(IClientContext context, CancellationToken ct)
            => Task.CompletedTask;
    }
}
