// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerProvider
    {
        private sealed class ProjectEntry(
            IDisposable watcher,
            string projectDirectory,
            string configKey)
        {
            public IDisposable Watcher { get; } = watcher;
            public string ProjectDirectory { get; } = projectDirectory;
            public string ConfigKey { get; } = configKey;
            public bool BuildDirsInjected { get; set; }
            public bool MissingMetadataNotified { get; set; }
            // Set after build settings change. Once .qmlls.build.ini is present and patched,
            // restart qmlls so it can read the updated build settings in a fresh process.
            public bool RestartWhenIniReady { get; set; }
            // Polls generated qmlls build settings. The files are build outputs and can be
            // rewritten by rebuild, clean+build, or command-line builds.
            public BuildSettingsMonitor? BuildSettingsMonitor { get; set; }
        }

        private readonly record struct FileSignature(
            bool Exists,
            DateTime LastWriteTimeUtc,
            long Length);

        private sealed class BuildSettingsMonitor : IDisposable
        {
            private readonly CancellationTokenSource cts = new();
            private Dictionary<string, FileSignature>? signatures;

            public CancellationToken Token => cts.Token;

            // Returns true after the initial snapshot when a monitored file signature changes.
            public bool UpdateSignatures(Dictionary<string, FileSignature> current)
            {
                if (signatures == null) {
                    signatures = current;
                    return false; // First-time initialization: no update
                }

                if (signatures.Count != current.Count) {
                    signatures = current;
                    return true; // Counts differ: update occurred
                }

                foreach (var entry in signatures) {
                    if (current.TryGetValue(entry.Key, out var value) && value.Equals(entry.Value))
                        continue;
                    signatures = current;
                    return true; // Value mismatch: update occurred
                }

                return false; // No differences found
            }

            public void Dispose()
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}
