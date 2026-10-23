// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Resolves Qt Bridge project metadata for arbitrary file or project paths by combining
    /// <see cref="IQtBridgeProjectFileLocator"/>, <see cref="IQtBridgeProjectStaticDetector"/> and
    /// <see cref="IQtBridgeProjectCapabilityDetector"/>.
    /// </summary>
    public sealed class QtBridgeProjectService(
        IQtBridgeProjectFileLocator locator,
        IQtBridgeProjectStaticDetector staticDetector,
        IQtBridgeProjectCapabilityDetector capabilityDetector)
        : IQtBridgeProjectService
    {
        private readonly IQtBridgeProjectFileLocator locator = locator
            ?? throw new ArgumentNullException(nameof(locator));
        private readonly IQtBridgeProjectStaticDetector staticDetector = staticDetector
            ?? throw new ArgumentNullException(nameof(staticDetector));
        private readonly IQtBridgeProjectCapabilityDetector capabilityDetector = capabilityDetector
            ?? throw new ArgumentNullException(nameof(capabilityDetector));

        public async Task<QtBridgeProjectMetadata?> TryGetMetadataForPathAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var projectFilePath = locator.FindEnclosingProjectFile(path);
            if (projectFilePath == null)
                return null;

            var metadata = await staticDetector.DetectAsync(projectFilePath, cancellationToken);
            var isTemplated = metadata.MatchedPackageId is { } packageId
                && QtBridgeProjectConstants.IsTemplatedQtBridgePackageReference(packageId);
            if (metadata.IsQtBridgeProject && !isTemplated)
                return metadata;

            // Static analysis misses package references from imports and cannot resolve the
            // templated package ID. The evaluated project has both, even before a restore.
            var result = await capabilityDetector.EvaluateAsync(
                metadata.ProjectFilePath,
                cancellationToken);
            return result.IsQtBridgeProject ? metadata.WithEvaluation(result) : metadata;
        }
    }
}
