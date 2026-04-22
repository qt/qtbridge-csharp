// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlMetadata
{
    /// <summary>
    /// Watches the project's <c>obj</c> directory tree for <c>qtbridge-qml.ide.json</c> changes
    /// by polling every two seconds. Polling is used instead of <see cref="FileSystemWatcher"/>
    /// because a .NET build generates hundreds of file-system events that overflow the watcher's
    /// kernel buffer and silently drop the metadata file creation event.
    /// </summary>
    internal sealed class QmlMetadataWatcher(IExtensionLog log)
        : IQmlMetadataWatcher
    {
        private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));

        public IDisposable Watch(string projectDir, string configuration, Action metadataAction)
        {
            if (string.IsNullOrWhiteSpace(projectDir)
                || string.IsNullOrWhiteSpace(configuration)
                || !Directory.Exists(projectDir)) {
                return Disposable.Empty;
            }
            return new MetadataFileWatcher(projectDir, configuration, metadataAction, log);
        }

        private sealed class MetadataFileWatcher : IDisposable
        {
            private const int PollIntervalMs = 2000;

            private static readonly QmlMetadataReader Reader = new();
            private readonly string projectDirectory;
            private readonly string configurationKey;
            private readonly Action metadataChanged;
            private readonly IExtensionLog extensionLog;
            private readonly CancellationTokenSource cts = new();
            private string? lastSignature;

            public MetadataFileWatcher(
                string projectDir,
                string configKey,
                Action metadataAction,
                IExtensionLog extensionLog)
            {
                projectDirectory = projectDir;
                configurationKey = configKey;
                metadataChanged = metadataAction;
                this.extensionLog = extensionLog;
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
                    try {
                        metadataChanged();
                    } catch (Exception ex) {
                        extensionLog.Error(
                            "QML metadata watcher: callback threw an exception.", ex);
                    }
                }
            }

            // Track last timestamp-read failure to avoid re-logging the same error every 2 s.
            private string? lastTimestampError;

            private string? GetMetadataFileTimestamp()
            {
                try {
                    var path = Reader.FindMetadataFilePath(projectDirectory, configurationKey);
                    lastTimestampError = null;
                    return path == null
                        ? null
                        : File.GetLastWriteTimeUtc(path).Ticks.ToString();
                } catch (Exception ex) {
                    var key = ex.GetType().Name + ": " + ex.Message;
                    if (key == lastTimestampError)
                        return null;

                    lastTimestampError = key;
                    extensionLog.Error("QML metadata watcher: could not read file timestamp.", ex);
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
