// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Microsoft.VisualStudio.ProjectSystem.Query;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext
{
    /// <summary>
    /// Path, capabilities and package references of a loaded project, read with the project
    /// query API.
    /// </summary>
    internal sealed class EvaluatedProject(
        string path,
        IEnumerable<string> capabilities,
        IReadOnlyList<EvaluatedProject.ActiveConfiguration> configurations)
    {
        internal sealed record ActiveConfiguration(IReadOnlyList<string> PackageIds);

        public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

        public IReadOnlyCollection<string> Capabilities { get; } =
            new HashSet<string>(capabilities, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<ActiveConfiguration> ActiveConfigurations { get; } =
            configurations ?? throw new ArgumentNullException(nameof(configurations));

        /// <summary>
        /// True once the project is restored and declares
        /// <see cref="QtBridgeProjectConstants.ProjectCapability"/>.
        /// </summary>
        public bool HasCapability =>
            Capabilities.Contains(QtBridgeProjectConstants.ProjectCapability);

        /// <summary>
        /// The Qt Bridge package that an active configuration references, or <c>null</c>. Known
        /// before a restore.
        /// </summary>
        public string? PackageId => ActiveConfigurations
            .SelectMany(configuration => configuration.PackageIds)
            .FirstOrDefault(QtBridgeProjectConstants.IsKnownQtBridgePackageId);

        /// <summary>
        /// Adds the project data that <see cref="FromSnapshot"/> reads to a project query.
        /// <see cref="EvaluatedProjectMonitor"/> tracks the same query, so extend it only here.
        /// </summary>
        public static IAsyncQueryable<IProjectSnapshot> WithEvaluatedData(
            IAsyncQueryable<IProjectSnapshot> projects) => projects
            .With(project => project.Path)
            .With(project => project.Capabilities)
            .With(project => project.ActiveConfigurations
                .With(configuration => configuration.PackageReferences
                    .With(reference => reference.Name)));

        /// <summary>
        /// Reads a snapshot queried with <see cref="WithEvaluatedData"/>. Returns <c>null</c> for
        /// a project without a path.
        /// </summary>
        public static EvaluatedProject? FromSnapshot(IProjectSnapshot project)
        {
            if (string.IsNullOrWhiteSpace(project.Path))
                return null;

            var configurations = project.ActiveConfigurations
                .Select(configuration => new ActiveConfiguration([ ..
                    configuration.PackageReferences.Select(reference => reference.Name)
                ]))
                .ToList();
            return new EvaluatedProject(System.IO.Path.GetFullPath(project.Path),
                project.Capabilities, configurations);
        }
    }
}
