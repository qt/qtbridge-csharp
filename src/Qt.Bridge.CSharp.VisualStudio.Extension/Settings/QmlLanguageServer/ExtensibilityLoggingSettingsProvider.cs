// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal sealed class ExtensibilityLoggingSettingsProvider(
        QmlLanguageServerLoggingCategoryObserver observer)
        : ILoggingSettingsProvider
    {
        private readonly QmlLanguageServerLoggingCategoryObserver observer = observer
            ?? throw new ArgumentNullException(nameof(observer));

        public async Task<LoggingOptions> GetOptionsAsync(CancellationToken ct)
        {
            try {
                var settings = await observer.GetSnapshotAsync(ct);

                var qmllsLogEnabled = settings.QmllsLogEnabled.ValueOrDefault(
                    LoggingContributions.QmllsLogEnabled.DefaultValue);
                var lspLogEnabled = settings.LspLogEnabled.ValueOrDefault(
                    LoggingContributions.LspLogEnabled.DefaultValue);

                return LoggingOptions.Create(
                    qmllsLogEnabled,
                    settings.QmllsLogDirectory.ValueOrDefault(
                        LoggingContributions.QmllsLogDirectory.DefaultValue),
                    lspLogEnabled,
                    settings.LspLogDirectory.ValueOrDefault(
                        LoggingContributions.LspLogDirectory.DefaultValue));
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                return LoggingOptions.Default;
            }
        }
    }
}
