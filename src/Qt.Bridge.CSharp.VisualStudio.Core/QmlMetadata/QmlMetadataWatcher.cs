// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    /// <summary>
    /// Watches the project's <c>obj</c> directory tree for <c>qtbridge-qml.ide.json</c> changes
    /// by polling every two seconds. Polling is used instead of <see cref="FileSystemWatcher"/>
    /// because a .NET build generates hundreds of file-system events that overflow the watcher's
    /// kernel buffer and silently drop the metadata file creation event.
    /// </summary>
    public sealed class QmlMetadataWatcher : IQmlMetadataWatcher
    {
        public IDisposable Watch(string projectDir, string configuration, Action metadataAction)
        {
            if (string.IsNullOrWhiteSpace(projectDir)
                || string.IsNullOrWhiteSpace(configuration)
                || !Directory.Exists(projectDir)) {
                return Disposable.Empty;
            }
            return new MetadataFileWatcher(projectDir, configuration, metadataAction);
        }

        private sealed class MetadataFileWatcher : IDisposable
        {
            private const int PollIntervalMs = 2000;

            private static readonly QmlMetadataReader Reader = new();
            private readonly string projectDirectory;
            private readonly string configurationKey;
            private readonly Action metadataChanged;
            private readonly CancellationTokenSource cts = new();
            private string? lastSignature;

            public MetadataFileWatcher(string projectDir, string configKey, Action metadataAction)
            {
                projectDirectory = projectDir;
                configurationKey = configKey;
                metadataChanged = metadataAction;
                lastSignature = GetMetadataFileTimestamp();

                _ = PollAsync(cts.Token);
            }

            public void Dispose()
            {
                cts.Cancel();
                cts.Dispose();
            }

            private async Task PollAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested) {
                    try {
                        await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);
                    } catch (OperationCanceledException) {
                        return;
                    }

                    var current = GetMetadataFileTimestamp();
                    if (current == lastSignature)
                        continue;
                    lastSignature = current;
                    metadataChanged();
                }
            }

            private string? GetMetadataFileTimestamp()
            {
                try {
                    var path = Reader.FindMetadataFilePath(projectDirectory, configurationKey);
                    return path == null ? null : File.GetLastWriteTimeUtc(path).Ticks.ToString();
                } catch {
                    return null;
                }
            }
        }

        private sealed class Disposable : IDisposable
        {
            public static readonly Disposable Empty = new();

            public void Dispose() { }
        }
    }
}
