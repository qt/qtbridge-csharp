// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext
{
    /// <summary>
    /// Implements <see cref="IProjectContextService"/> using the Visual Studio DTE automation
    /// model, subscribing to solution and document events to drive QML Language Server provider
    /// state.
    /// </summary>
    /// <remarks>
    /// TODO: Reduce DTE dependency once the VS Extensibility SDK matures:
    /// <list type="bullet">
    /// <item><description>
    /// Document-opened events (<see cref="DteContextSubscription"/> subscribes to
    /// <c>DocumentEvents.DocumentOpened</c>) can be replaced with
    /// <c>ITextViewOpenClosedListener</c>. Implement it on an <c>ExtensionPart</c>, set
    /// <c>TextViewExtensionConfiguration.AppliesTo</c> to filter on the <c>qml</c> document
    /// type, and receive <c>TextViewOpenedAsync</c> / <c>TextViewClosedAsync</c> callbacks.
    /// This is a stable, non-preview API and is the preferred replacement.
    /// </description></item>
    /// <item><description>
    /// Solution open/close: <c>ActivationConstraint.SolutionState(SolutionState.FullyLoaded)</c>
    /// exists in the SDK but applies only to command <c>EnabledWhen</c> / <c>VisibleWhen</c>
    /// gates, not to event subscriptions. There is no SDK-native push notification for solution
    /// open/close that can replace <c>SolutionEvents.Opened</c> / <c>AfterClosing</c> on a
    /// <c>LanguageServerProvider</c>. DTE remains required for this signal.
    /// </description></item>
    /// <item><description>
    /// Project add/remove/rename: <c>Extensibility.Workspaces().QueryProjectsAsync()</c> is a
    /// stable SDK API that can replace the DTE query in
    /// <see cref="GetLoadedProjectPathsAsync"/> today. Push-based change notifications
    /// (<c>TrackUpdatesAsync</c>) exist but are still marked
    /// <c>VSEXTPREVIEW_PROJECTQUERY_TRACKING</c> and are not yet production-ready. Tracked in
    /// https://github.com/microsoft/VSExtensibility/issues/392 (Backlog as of 2026-04).
    /// </description></item>
    /// </list>
    /// </remarks>
    internal sealed class DteProjectContextService : IProjectContextService
    {
        public IDisposable SubscribeToContextChanged(Action onContextChanged)
        {
            if (onContextChanged is null)
                throw new ArgumentNullException(nameof(onContextChanged));
            // Synchronous entrypoint required by IProjectContextService contract.
#pragma warning disable VSTHRD102
            return ThreadHelper.JoinableTaskFactory.Run(() => SubscribeCoreAsync(onContextChanged));
#pragma warning restore VSTHRD102
        }

        public async Task<string?> GetActiveProjectPathAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (GetDte() is not { } dte)
                return null;

            if (dte.ActiveSolutionProjects is not Array activeSolutionProjects)
                return null;

            return activeSolutionProjects.Length == 0
                ? null : (activeSolutionProjects.GetValue(0) as Project)?.FullName;
        }

        public async Task<string?> GetActiveDocumentPathAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            return GetDte()?.ActiveDocument?.FullName;
        }

        public async Task<string?> GetActiveConfigurationAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (GetDte() is not { } dte)
                return null;

            try {
                return dte.Solution?.SolutionBuild?.ActiveConfiguration?.Name;
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                return null;
            }
        }

        public async Task<string?> GetActivePlatformAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (GetDte() is not { } dte)
                return null;

            try {
                return (dte.Solution?.SolutionBuild?.ActiveConfiguration
                    as SolutionConfiguration2)?.PlatformName;
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                return null;
            }
        }

        public async Task<string?> GetOwningProjectPathAsync(string filePath, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            if (GetDte() is not { } dte || string.IsNullOrWhiteSpace(filePath))
                return null;

            try {
                return dte.Solution?.FindProjectItem(filePath)
                    ?.ContainingProject?.FullName;
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                _ = ex;
                return null;
            }
        }

        public async Task<IReadOnlyList<string>> GetLoadedProjectPathsAsync(CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            if (GetDte() is not { } dte || dte.Solution is null)
                return [];

            var projectPaths = new List<string>();
            foreach (Project project in dte.Solution.Projects) {
                ct.ThrowIfCancellationRequested();
                AddProjectPaths(project, projectPaths);
            }

            return [..projectPaths
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
        }

        private static async Task<IDisposable> SubscribeCoreAsync(Action onContextChanged)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (GetDte() is not { } dte)
                return EmptyDisposable.Instance;

            return new DteContextSubscription(dte, onContextChanged);
        }

        private static DTE2? GetDte()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        private static void AddProjectPaths(Project? project, ICollection<string> projectPaths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project is null)
                return;

            if (!string.IsNullOrWhiteSpace(project.FullName))
                projectPaths.Add(project.FullName);

            if (project.ProjectItems is null)
                return;

            foreach (ProjectItem item in project.ProjectItems) {
                if (item.SubProject is { } subProject)
                    AddProjectPaths(subProject, projectPaths);
            }
        }

        private sealed class DteContextSubscription : IDisposable
        {
            private const int DebounceDelayMs = 250;
            private readonly SolutionEvents solutionEvents;
            private readonly DocumentEvents documentEvents;
            private readonly Timer debounceTimer;
            private readonly object gate = new();
            private bool disposed;

            public DteContextSubscription(DTE2 dte, Action onContextChanged)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var onContextChanged1 = onContextChanged;
                debounceTimer = new Timer(_ => onContextChanged1(), null, Timeout.Infinite,
                    Timeout.Infinite);
                solutionEvents = dte.Events.SolutionEvents;
                documentEvents = dte.Events.DocumentEvents;

                solutionEvents.Opened += OnContextChanged;
                solutionEvents.AfterClosing += OnContextChanged;
                solutionEvents.ProjectAdded += OnProjectChanged;
                solutionEvents.ProjectRemoved += OnProjectChanged;
                solutionEvents.ProjectRenamed += OnProjectRenamed;
                documentEvents.DocumentOpened += OnDocumentChanged;
            }

            public void Dispose()
            {
                lock (gate)
                    disposed = true;

                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    solutionEvents.Opened -= OnContextChanged;
                    solutionEvents.AfterClosing -= OnContextChanged;
                    solutionEvents.ProjectAdded -= OnProjectChanged;
                    solutionEvents.ProjectRemoved -= OnProjectChanged;
                    solutionEvents.ProjectRenamed -= OnProjectRenamed;
                    documentEvents.DocumentOpened -= OnDocumentChanged;
                });

                debounceTimer.Dispose();
            }

            private void OnContextChanged() => ScheduleContextRefresh();
            private void OnProjectChanged(Project project) => ScheduleContextRefresh();
            private void OnProjectRenamed(Project project, string oldName) =>
                ScheduleContextRefresh();
            private void OnDocumentChanged(Document document) => ScheduleContextRefresh();

            private void ScheduleContextRefresh()
            {
                lock (gate) {
                    if (disposed)
                        return;
                    debounceTimer.Change(DebounceDelayMs, Timeout.Infinite);
                }
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose()
            {}
        }
    }
}
