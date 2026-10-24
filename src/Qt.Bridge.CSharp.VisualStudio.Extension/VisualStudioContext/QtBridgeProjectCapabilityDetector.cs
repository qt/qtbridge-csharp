// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.IO;
using Microsoft.VisualStudio.Extensibility;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext
{
    /// <summary>
    /// Implements <see cref="IQtBridgeProjectCapabilityDetector"/> with the Visual Studio project
    /// query API.
    /// </summary>
    /// <remarks>
    /// Reads the projects from the data that <see cref="EvaluatedProjectMonitor"/> tracks. Before
    /// the first update and without tracking, it queries on each call.
    /// </remarks>
    internal sealed class QtBridgeProjectCapabilityDetector(
        VisualStudioExtensibility vsExt,
        EvaluatedProjectMonitor projectMonitor,
        IExtensionLog extensionLog)
        : IQtBridgeProjectCapabilityDetector
    {
        /// <summary> The result of <see cref="GetEvaluatedProjectAsync"/>. </summary>
        /// <param name="Project">The project, or <c>null</c> if it is not loaded.</param>
        /// <param name="FromTracking">Whether the project data came from the tracked data.</param>
        /// <param name="IsTracking">Whether project updates were tracked.</param>
        /// <param name="Version">
        /// <see cref="EvaluatedProjectMonitor.Version"/> at the time of the lookup.
        /// </param>
        /// <param name="Error">The message of a failed query.</param>
        internal sealed record Lookup(
            EvaluatedProject? Project,
            bool FromTracking,
            bool IsTracking,
            int Version,
            string? Error = null);

        private readonly VisualStudioExtensibility extensibility = vsExt
            ?? throw new ArgumentNullException(nameof(vsExt));
        private readonly EvaluatedProjectMonitor monitor = projectMonitor
            ?? throw new ArgumentNullException(nameof(projectMonitor));
        private readonly IExtensionLog log = extensionLog
            ?? throw new ArgumentNullException(nameof(extensionLog));

        public async Task<EvaluationResult> EvaluateAsync(
            string projectFilePath,
            CancellationToken cancellationToken = default)
        {
            var project = (await GetEvaluatedProjectAsync(projectFilePath, cancellationToken))
                .Project;
            return project == null
                ? EvaluationResult.None
                : new EvaluationResult(project.HasCapability, project.PackageId);
        }

        /// <summary>
        /// Looks up the evaluated project like <see cref="EvaluateAsync"/> and returns it with the
        /// tracking state.
        /// </summary>
        public async Task<Lookup> GetEvaluatedProjectAsync(
            string projectFilePath,
            CancellationToken cancellationToken = default)
        {
            monitor.EnsureStarted();

            var isTracking = monitor.IsTracking;
            var version = monitor.Version;
            if (string.IsNullOrWhiteSpace(projectFilePath))
                return new(null, FromTracking: false, isTracking, version);

            try {
                var fullPath = Path.GetFullPath(projectFilePath);
                if (monitor.TryGetProject(fullPath, out var tracked))
                    return new(tracked, FromTracking: true, isTracking, version);

                var projects = await QueryEvaluatedProjectsAsync(cancellationToken);
                return new(projects.TryGetValue(fullPath, out var project) ? project : null,
                    FromTracking: false, isTracking, version);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                // Detection must not fail because of the project system; the project then
                // counts as not detected.
                log.Warning($"Project query failed for '{projectFilePath}': " + ex.Message);
                return new(null, FromTracking: false, isTracking, version, ex.Message);
            }
        }

        private async Task<Dictionary<string, EvaluatedProject>> QueryEvaluatedProjectsAsync(
            CancellationToken ct)
        {
            var snapshots = await extensibility.Workspaces().QueryProjectsAsync(
                EvaluatedProject.WithEvaluatedData, ct);

            // The project system may report a differently cased path.
            var projects = new Dictionary<string, EvaluatedProject>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var snapshot in snapshots) {
                if (EvaluatedProject.FromSnapshot(snapshot) is { } project)
                    projects[project.Path] = project;
            }
            return projects;
        }
    }
}
