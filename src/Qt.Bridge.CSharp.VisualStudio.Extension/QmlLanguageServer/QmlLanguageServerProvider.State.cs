// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerProvider
    {
        // Mutable provider/session state is guarded by registryLock. Helpers in this partial
        // perform complete state transitions and return snapshots or detached resources so
        // callers never hold the lock while awaiting or disposing.
        private readonly object registryLock = new();
        private readonly Dictionary<string, ProjectEntry> projectRegistry = [];
        private bool registryDisposed;
        private QmlLanguageServerTransportPipe? activePipe;

        private enum ProjectRegistrationNeed
        {
            Current,
            Required,
            RegistryDisposed
        }

        private enum ProjectRegistrationResult
        {
            Current,
            Registered,
            RegistryDisposed
        }

        private readonly record struct ProjectRegistration(
            ProjectRegistrationResult Result,
            ProjectEntry? DisplacedEntry);

        private readonly record struct BuildSettingsStateUpdate(
            bool ProjectRegistered,
            bool ShouldRetry,
            bool ShouldLogChange);

        private readonly record struct RemovedProject(
            string ProjectFilePath,
            ProjectEntry Entry);

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

        private List<string> ActivatePipe(QmlLanguageServerTransportPipe pipe)
        {
            lock (registryLock) {
                activePipe = pipe;
                foreach (var entry in projectRegistry.Values)
                    entry.BuildDirsInjected = false;
                return [..projectRegistry.Keys];
            }
        }

        private List<ProjectEntry> DisposeRegistry()
        {
            lock (registryLock) {
                registryDisposed = true;
                activePipe = null;
                var entries = projectRegistry.Values.ToList();
                projectRegistry.Clear();
                return entries;
            }
        }

        private ProjectRegistrationNeed IsProjectRegistrationNeeded(
            string projectFilePath,
            string configKey)
        {
            lock (registryLock) {
                if (registryDisposed)
                    return ProjectRegistrationNeed.RegistryDisposed;
                return !projectRegistry.TryGetValue(projectFilePath, out var existing)
                    || existing.ConfigKey != configKey
                        ? ProjectRegistrationNeed.Required
                        : ProjectRegistrationNeed.Current;
            }
        }

        private ProjectRegistration RegisterProject(
            string projectFilePath,
            string projectDirectory,
            string configKey,
            IDisposable watcher)
        {
            lock (registryLock) {
                if (registryDisposed) {
                    return new ProjectRegistration(
                        ProjectRegistrationResult.RegistryDisposed, null);
                }

                if (projectRegistry.TryGetValue(projectFilePath, out var existing)
                    && existing.ConfigKey == configKey) {
                    return new ProjectRegistration(
                        ProjectRegistrationResult.Current, null);
                }

                projectRegistry[projectFilePath] =
                    new ProjectEntry(watcher, projectDirectory, configKey);
                return new ProjectRegistration(
                    ProjectRegistrationResult.Registered, existing);
            }
        }

        private bool TryBeginProjectInjection(
            string projectFilePath,
            out QmlLanguageServerTransportPipe pipe,
            out string projectDirectory,
            out string configKey)
        {
            lock (registryLock) {
                pipe = activePipe!;
                projectDirectory = string.Empty;
                configKey = string.Empty;

                if (activePipe == null)
                    return false;
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return false;
                if (entry.BuildDirsInjected)
                    return false;

                // Claim the slot before async work starts so concurrent calls cannot inject
                // the same project into one server session.
                entry.BuildDirsInjected = true;
                pipe = activePipe;
                projectDirectory = entry.ProjectDirectory;
                configKey = entry.ConfigKey;
                return true;
            }
        }

        private void ResetInjection(string projectFilePath)
        {
            lock (registryLock) {
                if (projectRegistry.TryGetValue(projectFilePath, out var entry))
                    entry.BuildDirsInjected = false;
            }
        }

        private bool ResetInjectionAndShouldCheckNotification(
            string projectFilePath,
            bool notifyUser)
        {
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return false;

                entry.BuildDirsInjected = false;
                return notifyUser && !entry.MissingMetadataNotified;
            }
        }

        private bool TryMarkMissingMetadataNotified(string projectFilePath)
        {
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry)
                    || entry.MissingMetadataNotified) {
                    return false;
                }

                entry.MissingMetadataNotified = true;
                return true;
            }
        }

        private bool TryConsumePendingRestart(string projectFilePath)
        {
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry)
                    || !entry.RestartWhenIniReady) {
                    return false;
                }

                entry.RestartWhenIniReady = false;
                entry.BuildDirsInjected = false;
                return true;
            }
        }

        private bool CompleteProjectInjection(
            string projectFilePath,
            QmlLanguageServerTransportPipe pipe)
        {
            lock (registryLock) {
                if (!ReferenceEquals(activePipe, pipe)) {
                    if (projectRegistry.TryGetValue(projectFilePath, out var staleEntry))
                        staleEntry.BuildDirsInjected = false;
                    return false;
                }

                if (projectRegistry.TryGetValue(projectFilePath, out var entry))
                    entry.MissingMetadataNotified = false;
                return true;
            }
        }

        private QmlLanguageServerTransportPipe? GetActivePipe()
        {
            lock (registryLock)
                return activePipe;
        }

        private void ClearActivePipe()
        {
            lock (registryLock)
                activePipe = null;
        }

        private bool TryCreateBuildSettingsMonitor(
            string projectFilePath,
            out BuildSettingsMonitor monitor)
        {
            lock (registryLock) {
                monitor = null!;
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return false;
                if (entry.BuildSettingsMonitor != null)
                    return false;

                monitor = new BuildSettingsMonitor();
                entry.BuildSettingsMonitor = monitor;
                return true;
            }
        }

        private BuildSettingsStateUpdate UpdateBuildSettingsState(
            string projectFilePath,
            bool changed,
            bool hasSignatures)
        {
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return new BuildSettingsStateUpdate(false, false, false);

                var shouldLogChange = changed && entry.BuildDirsInjected;
                if (shouldLogChange) {
                    entry.BuildDirsInjected = false;
                    entry.RestartWhenIniReady = true;
                }

                var shouldRetry = changed || (!entry.BuildDirsInjected && hasSignatures);
                return new BuildSettingsStateUpdate(true, shouldRetry, shouldLogChange);
            }
        }

        private bool TryGetProjectContext(
            string projectFilePath,
            out string projectDirectory,
            out string configKey)
        {
            lock (registryLock) {
                projectDirectory = string.Empty;
                configKey = string.Empty;
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return false;

                projectDirectory = entry.ProjectDirectory;
                configKey = entry.ConfigKey;
                return true;
            }
        }

        private bool MarkProjectMetadataChanged(string projectFilePath)
        {
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return false;

                entry.BuildDirsInjected = false;
                entry.RestartWhenIniReady = true;
                return true;
            }
        }

        private List<RemovedProject> RemoveUnloadedProjects(ISet<string> loadedProjects)
        {
            lock (registryLock) {
                var removedProjects = new List<RemovedProject>();
                foreach (var project in projectRegistry.ToList()) {
                    if (loadedProjects.Contains(project.Key))
                        continue;

                    projectRegistry.Remove(project.Key);
                    removedProjects.Add(new RemovedProject(project.Key, project.Value));
                }
                return removedProjects;
            }
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
