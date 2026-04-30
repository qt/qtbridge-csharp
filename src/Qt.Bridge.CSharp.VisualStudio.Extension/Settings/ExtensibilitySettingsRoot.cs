// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings
{
    internal static class ExtensibilitySettingsRoot
    {
        [VisualStudioContribution]
        internal static SettingCategory RootCategory { get; } = new("qtBridge",
            "%QtBridge.Settings.Root.DisplayName%");
    }
}
