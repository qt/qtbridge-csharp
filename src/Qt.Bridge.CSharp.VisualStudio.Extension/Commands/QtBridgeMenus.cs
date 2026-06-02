// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    internal static class QtBridgeMenus
    {
        [VisualStudioContribution]
        internal static MenuConfiguration QtBridgeMenu => new("%QtBridge.Menu.DisplayName%")
        {
            Placements =
            [
                CommandPlacement.KnownPlacements.ExtensionsMenu.WithPriority(0x100),
            ],
            Children =
            [
                MenuChild.Command<Options>(),
#if DEBUG
                MenuChild.Separator,
                MenuChild.Command<ShowStatus>(),
#endif
                MenuChild.Separator,
                MenuChild.Command<Version>()
            ],
        };
    }
}
