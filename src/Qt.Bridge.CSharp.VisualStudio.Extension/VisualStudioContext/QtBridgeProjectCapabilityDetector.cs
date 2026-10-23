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
    internal sealed class QtBridgeProjectCapabilityDetector(
        VisualStudioExtensibility vsExt,
        IExtensionLog extensionLog)
        : IQtBridgeProjectCapabilityDetector
    {
        private readonly VisualStudioExtensibility extensibility = vsExt
            ?? throw new ArgumentNullException(nameof(vsExt));
        private readonly IExtensionLog log = extensionLog
            ?? throw new ArgumentNullException(nameof(extensionLog));

        public async Task<EvaluationResult> EvaluateAsync(
            string projectFilePath,
            CancellationToken cancellationToken = default)
        {
            var project = await FindEvaluatedProjectAsync(projectFilePath, cancellationToken);
            return project == null
                ? EvaluationResult.None
                : new EvaluationResult(project.HasCapability, project.PackageId);
        }

        private async Task<EvaluatedProject?> FindEvaluatedProjectAsync(
            string projectFilePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectFilePath))
                return null;

            try {
                var fullPath = Path.GetFullPath(projectFilePath);
                var projects = await QueryEvaluatedProjectsAsync(cancellationToken);
                return projects.TryGetValue(fullPath, out var project) ? project : null;
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                // Detection must not fail because of the project system; the project then
                // counts as not detected.
                log.Warning($"Project query failed for '{projectFilePath}': " + ex.Message);
                return null;
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
