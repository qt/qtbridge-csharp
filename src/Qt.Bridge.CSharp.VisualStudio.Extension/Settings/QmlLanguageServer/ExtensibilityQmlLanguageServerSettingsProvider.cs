// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer
{
    internal sealed class ExtensibilityQmlLanguageServerSettingsProvider
        : IQmlLanguageServerSettingsProvider
    {
        private readonly QmlLanguageServerCategoryObserver observer;

        public ExtensibilityQmlLanguageServerSettingsProvider(
            QmlLanguageServerCategoryObserver observer)
        {
            this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
            this.observer.Changed += OnObserverChangedAsync;
        }

        public event Action? Changed;

        public async Task<QmlLanguageServerOptions> GetOptionsAsync(CancellationToken ct)
        {
            try {
                var settings = await observer.GetSnapshotAsync(ct);

                var qmllsLogEnabled = settings.QmllsLogEnabled.ValueOrDefault(
                    QmlLanguageServerContributions.QmllsLogEnabled.DefaultValue);
                var lspLogEnabled = settings.LspLogEnabled.ValueOrDefault(
                    QmlLanguageServerContributions.LspLogEnabled.DefaultValue);

                return new QmlLanguageServerOptions(
                    SemanticTokenDeltasEnabled: settings.SemanticTokenDeltasEnabled.ValueOrDefault(
                        QmlLanguageServerContributions.SemanticTokenDeltasEnabled.DefaultValue),
                    Logging: LoggingOptions.Create(
                        qmllsLogEnabled,
                        settings.QmllsLogDirectory.ValueOrDefault(
                            QmlLanguageServerContributions.QmllsLogDirectory.DefaultValue),
                        lspLogEnabled,
                        settings.LspLogDirectory.ValueOrDefault(
                            QmlLanguageServerContributions.LspLogDirectory.DefaultValue)));
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                return QmlLanguageServerOptions.Default;
            }
        }

        private Task OnObserverChangedAsync(QmlLanguageServerCategorySnapshot snapshot)
        {
            Changed?.Invoke();
            return Task.CompletedTask;
        }
    }
}
