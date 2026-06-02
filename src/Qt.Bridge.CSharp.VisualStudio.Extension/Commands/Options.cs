// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    [VisualStudioContribution]
    internal sealed class Options : Command
    {
        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.Options.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.Settings,
                    IconSettings.IconAndText)
            };

        public override async Task ExecuteCommandAsync(IClientContext ctx, CancellationToken ct) =>
            await VisualStudioVersion.OpenSettingsAsync(ct);
    }
}
