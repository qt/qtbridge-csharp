// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

using ShellSrvProvider = Microsoft.VisualStudio.Shell.ServiceProvider;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    [VisualStudioContribution]
    internal sealed class ExtensionEntrypoint : Microsoft.VisualStudio.Extensibility.Extension
    {
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            RequiresInProcessHosting = true
        };

        protected override void InitializeServices(IServiceCollection serviceCollection)
        {
            if (VisualStudioVersion.SupportsVisualStudioExtensibility())
                serviceCollection.AddSettingsObservers();
            base.InitializeServices(serviceCollection);

            serviceCollection.AddSingleton<IExtensionLog, ExtensionLog>();
            serviceCollection.AddSingleton<INotificationService, NotificationService>();
            serviceCollection.AddSingleton<IToastNotificationService, ToastNotificationService>();
            serviceCollection.AddSingleton(_ => new SettingsStore(ShellSrvProvider.GlobalProvider));
            serviceCollection.AddSingleton<IQmlBuildNotificationSettings,
                QmlBuildNotificationSettings>();

            serviceCollection.AddSingleton<IQtBridgeProjectDetector, QtBridgeProjectDetector>();
            serviceCollection.AddSingleton<IQtBridgeProjectFileLocator,
                QtBridgeProjectFileLocator>();
            serviceCollection.AddSingleton<IQtBridgeProjectService, QtBridgeProjectService>();

            serviceCollection.AddSingleton<IReleaseMetadataClient, ReleaseMetadataClient>();
            serviceCollection.AddSingleton<IZipArchiveExtractor, ZipArchiveExtractor>();
            serviceCollection.AddSingleton<IQmlLanguageServerInstaller,
                    QmlLanguageServerInstaller>();

            serviceCollection.AddSingleton<IQmlMetadataReader, QmlMetadataReader>();
            serviceCollection.AddSingleton<IQmlMetadataWatcher, QmlMetadataWatcher>();

            serviceCollection.AddSingleton<IProjectContextService, DteProjectContextService>();

            if (VisualStudioVersion.SupportsVisualStudioExtensibility()) {
                // VS 2026+: use the source-generated settings observer.
                serviceCollection.AddSingleton<ExtensibilityLoggingSettingsProvider>();
                serviceCollection.AddSingleton<ILoggingSettingsProvider>(sp =>
                    sp.GetRequiredService<ExtensibilityLoggingSettingsProvider>());
            } else {
                // VS 2022: the new extensibility settings UI is not supported; use DialogPage.
                serviceCollection.AddSingleton<ILoggingSettingsProvider,
                    VsSdkLoggingSettingsProvider>();
            }
        }
    }
}
