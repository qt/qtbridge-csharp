// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Settings
{
    internal sealed class SettingsStore
    {
        private const string RootCollectionPath = "QtBridge";
        private const string WhatsNewCollectionPath = RootCollectionPath + "\\WhatsNew";
        private const string LastSeenVersionProperty = "LastSeenVersion";

        private readonly WritableSettingsStore store;

        public SettingsStore(IServiceProvider serviceProvider)
        {
            var manager = new ShellSettingsManager(serviceProvider);
            store = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        public string? LastSeenVersion
        {
            get
            {
                EnsureWhatsNewCollection();
                return store.PropertyExists(WhatsNewCollectionPath, LastSeenVersionProperty)
                    ? store.GetString(WhatsNewCollectionPath, LastSeenVersionProperty)
                    : null;
            }
            set
            {
                EnsureWhatsNewCollection();
                store.SetString(WhatsNewCollectionPath, LastSeenVersionProperty, value ?? "");
            }
        }

        public void Reset()
        {
            if (store.CollectionExists(RootCollectionPath))
                store.DeleteCollection(RootCollectionPath);
        }

        private void EnsureWhatsNewCollection()
        {
            if (!store.CollectionExists(WhatsNewCollectionPath))
                store.CreateCollection(WhatsNewCollectionPath);
        }
    }
}
