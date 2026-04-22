// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

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
            base.InitializeServices(serviceCollection);

            serviceCollection.AddSingleton<IExtensionLog, ExtensionLog>();
            serviceCollection.AddSingleton<INotificationService, NotificationService>();

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
        }
    }
}
