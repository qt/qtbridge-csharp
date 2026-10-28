// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal static class QmlLanguageServerContributions
    {
        [VisualStudioContribution]
        internal static SettingCategory QmlLanguageServerCategory { get; } =
            new("qtBridgeQmlLanguageServer",
                "%QtBridge.Settings.QmlLanguageServer.Category.Title%",
                ExtensibilitySettingsRoot.RootCategory)
            {
                Description = "%QtBridge.Settings.QmlLanguageServer.Category.Description%",
                GenerateObserverClass = true
            };

        [VisualStudioContribution]
        internal static Setting.Boolean QmllsLogEnabled { get; } =
            new("qmllsLogEnabled",
                "%QtBridge.Settings.QmllsLogEnabled.Title%",
                QmlLanguageServerCategory,
                defaultValue: false)
            {
                Description = "%QtBridge.Settings.QmllsLogEnabled.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.FormattedString QmllsLogDirectory { get; } =
            new("qmllsLogDirectory",
                "%QtBridge.Settings.QmllsLogDirectory.Title%",
                QmlLanguageServerCategory,
                SettingStringFormat.DirectoryPath,
                defaultValue: string.Empty)
            {
                Description = "%QtBridge.Settings.QmllsLogDirectory.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.Boolean LspLogEnabled { get; } =
            new("lspLogEnabled",
                "%QtBridge.Settings.LspLogEnabled.Title%",
                QmlLanguageServerCategory,
                defaultValue: false)
            {
                Description = "%QtBridge.Settings.LspLogEnabled.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.FormattedString LspLogDirectory { get; } =
            new("lspLogDirectory",
                "%QtBridge.Settings.LspLogDirectory.Title%",
                QmlLanguageServerCategory,
                SettingStringFormat.DirectoryPath,
                defaultValue: string.Empty)
            {
                Description = "%QtBridge.Settings.LspLogDirectory.Description%"
            };

        [VisualStudioContribution]
        internal static Setting.Boolean SemanticTokenDeltasEnabled { get; } =
            new("semanticTokenDeltasEnabled",
                "%QtBridge.Settings.SemanticTokenDeltasEnabled.Title%",
                QmlLanguageServerCategory,
                defaultValue: false)
            {
                Description = "%QtBridge.Settings.SemanticTokenDeltasEnabled.Description%"
            };
    }
}
