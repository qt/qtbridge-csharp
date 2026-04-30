// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal static class LoggingContributions
    {
        [VisualStudioContribution]
        internal static SettingCategory QmlLanguageServerLoggingCategory { get; } =
            new("qtBridgeQmlLanguageServerLogging",
                "%QtBridge.Settings.QmlLanguageServerLogging.Category.Title%",
                ExtensibilitySettingsRoot.RootCategory)
            {
                Description = "%QtBridge.Settings.QmlLanguageServerLogging.Category.Description%",
                GenerateObserverClass = true
            };

        [VisualStudioContribution]
        internal static Setting.Boolean QmllsLogEnabled { get; } =
            new("qmllsLogEnabled",
                "%QtBridge.Settings.QmllsLogEnabled.Title%",
                QmlLanguageServerLoggingCategory,
                defaultValue: false)
            {
                Description = "%QtBridge.Settings.QmllsLogEnabled.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.FormattedString QmllsLogDirectory { get; } =
            new("qmllsLogDirectory",
                "%QtBridge.Settings.QmllsLogDirectory.Title%",
                QmlLanguageServerLoggingCategory,
                SettingStringFormat.DirectoryPath,
                defaultValue: string.Empty)
            {
                Description = "%QtBridge.Settings.QmllsLogDirectory.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.Boolean LspLogEnabled { get; } =
            new("lspLogEnabled",
                "%QtBridge.Settings.LspLogEnabled.Title%",
                QmlLanguageServerLoggingCategory,
                defaultValue: false)
            {
                Description = "%QtBridge.Settings.LspLogEnabled.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.FormattedString LspLogDirectory { get; } =
            new("lspLogDirectory",
                "%QtBridge.Settings.LspLogDirectory.Title%",
                QmlLanguageServerLoggingCategory,
                SettingStringFormat.DirectoryPath,
                defaultValue: string.Empty)
            {
                Description = "%QtBridge.Settings.LspLogDirectory.Description%"
            };
    }
}
