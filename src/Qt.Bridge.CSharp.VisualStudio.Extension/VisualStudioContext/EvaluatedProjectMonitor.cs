// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

#pragma warning disable VSEXTPREVIEW_PROJECTQUERY_TRACKING

namespace Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext
{
    /// <summary>
    /// Tracks the <see cref="EvaluatedProject"/> data of the loaded projects with the project
    /// query API and reports each change.
    /// </summary>
    /// <remarks>
    /// The project system reports an update only after it has evaluated the project again, so a
    /// query after an update already sees the new data.
    /// </remarks>
    internal sealed class EvaluatedProjectMonitor(
        VisualStudioExtensibility vsExt,
        IExtensionLog extensionLog)
        : IDisposable
    {
        private readonly VisualStudioExtensibility extensibility = vsExt
            ?? throw new ArgumentNullException(nameof(vsExt));
        private readonly IExtensionLog log = extensionLog
            ?? throw new ArgumentNullException(nameof(extensionLog));
        private readonly CancellationTokenSource cancellation = new();
        private readonly object stateLock = new();
        private Observer? observer;
        private bool disposed;
        private IDisposable? subscription;
        private volatile bool isTracking;
        private int version;
        // Filled from the updates. The first update reports all loaded projects.
        private readonly Dictionary<EntityIdentity, EvaluatedProject> projectsById = [];
        private readonly Dictionary<string, EvaluatedProject> projectsByPath =
            new(StringComparer.OrdinalIgnoreCase);
        private bool hasProjects;

        /// <summary>
        /// Raised on a background thread when the <see cref="EvaluatedProject"/> data of a loaded
        /// project changes.
        /// </summary>
        public event Action? ProjectsChanged;

        /// <summary>
        /// False before the start and after the tracking failed or completed.
        /// </summary>
        public bool IsTracking => isTracking;

        /// <summary>
        /// Incremented on each update and when the tracking stops. Caches compare it to detect
        /// stale data.
        /// </summary>
        public int Version => Volatile.Read(ref version);

        /// <summary>
        /// Looks up a project in the tracked data. Returns <c>false</c> before the first update
        /// and after the tracking stopped. Returns <c>true</c> with a <c>null</c> project for a
        /// project that is not loaded.
        /// </summary>
        public bool TryGetProject(string fullPath, out EvaluatedProject? project)
        {
            lock (stateLock) {
                project = null;
                if (!hasProjects)
                    return false;
                projectsByPath.TryGetValue(fullPath, out project);
                return true;
            }
        }

        /// <summary>
        /// Starts tracking in the background, so that no caller waits for it. Restarted, for
        /// example when the solution was closed.
        /// </summary>
        public void EnsureStarted()
        {
            Observer current;
            lock (stateLock) {
                if (observer != null || disposed)
                    return;
                current = observer = new Observer(this);
            }
            _ = Task.Run(() => StartAsync(current));
        }

        public void Dispose()
        {
            Observer? current;
            lock (stateLock) {
                disposed = true;
                current = observer;
            }
            if (current != null)
                StopTracking(current);

            // Do not dispose it: a pending start may still read its token.
            cancellation.Cancel();
        }

        private async Task StartAsync(Observer current)
        {
            IDisposable tracking;
            try {
                var space = new ProjectQueryableSpace(extensibility.ServiceBroker,
                    ThreadHelper.JoinableTaskContext);
                tracking = await EvaluatedProject.WithEvaluatedData(space.Projects)
                    .TrackUpdatesAsync(current, cancellation.Token);
            } catch (OperationCanceledException) {
                return;
            } catch (Exception ex) {
                // The observer stays set, so the next lookup does not retry and log this again.
                log.Warning("Project tracking is unavailable: " + ex.Message
                    + " Changes of loaded projects are only picked up on the next context "
                    + "change, for example when a document is opened.");
                return;
            }

            lock (stateLock) {
                if (observer == current) {
                    subscription = tracking;
                    isTracking = true;
                    log.Info("Tracking project updates.");
                    return;
                }
            }
            // The tracking ended or was disposed while it started.
            tracking.Dispose();
        }

        private void StopTracking(Observer source)
        {
            IDisposable? tracking;
            lock (stateLock) {
                if (observer != source)
                    return;
                observer = null;
                isTracking = false;
                Interlocked.Increment(ref version);
                projectsById.Clear();
                projectsByPath.Clear();
                hasProjects = false;
                tracking = subscription;
                subscription = null;
            }
            tracking?.Dispose();
        }

        // Each update carries the queried data of a changed project, so the tracked data needs
        // no further query.
        private bool ApplyUpdates(
            Observer source,
            IEnumerable<ItemUpdate<IProjectSnapshot>> updates)
        {
            lock (stateLock) {
                if (observer != source)
                    return false;

                foreach (var update in updates) {
                    if (update.PreviousId is { } previousId
                        && projectsById.TryGetValue(previousId, out var previous)) {
                        projectsById.Remove(previousId);
                        // An added project with the same path may already have replaced it.
                        if (projectsByPath.TryGetValue(previous.Path, out var byPath)
                            && ReferenceEquals(byPath, previous)) {
                            projectsByPath.Remove(previous.Path);
                        }
                    }

                    if (update.Current is not { } snapshot)
                        continue;
                    if (EvaluatedProject.FromSnapshot(snapshot) is not { } project)
                        continue;

                    projectsById[snapshot.Id] = project;
                    projectsByPath[project.Path] = project;
                }
                hasProjects = true;
                Interlocked.Increment(ref version);
                return true;
            }
        }

        private sealed class Observer(EvaluatedProjectMonitor monitor)
            : IObserver<IQueryTrackUpdates<IProjectSnapshot>>
        {
            public void OnNext(IQueryTrackUpdates<IProjectSnapshot> value)
            {
                if (value.Updates.Count == 0 || !monitor.ApplyUpdates(this, value.Updates))
                    return;
                monitor.ProjectsChanged?.Invoke();
            }

            public void OnError(Exception error)
            {
                monitor.StopTracking(this);
                monitor.log.Warning("Project tracking stopped: " + error.Message);
            }

            public void OnCompleted()
            {
                monitor.StopTracking(this);
                monitor.log.Info("Project tracking completed.");
            }
        }
    }
}
