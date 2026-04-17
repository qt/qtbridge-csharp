// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Resolves Qt Bridge project metadata for arbitrary file or project paths by combining
    /// <see cref="IQtBridgeProjectFileLocator"/> and <see cref="IQtBridgeProjectDetector"/>.
    /// </summary>
    public sealed class QtBridgeProjectService(
        IQtBridgeProjectFileLocator locator,
        IQtBridgeProjectDetector detector)
        : IQtBridgeProjectService
    {
        private readonly IQtBridgeProjectFileLocator locator = locator
            ?? throw new ArgumentNullException(nameof(locator));
        private readonly IQtBridgeProjectDetector detector = detector
            ?? throw new ArgumentNullException(nameof(detector));

        public async Task<QtBridgeProjectMetadata?> TryGetMetadataForPathAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var projectFilePath = locator.FindEnclosingProjectFile(path);
            if (projectFilePath == null)
                return null;
            return await detector.DetectAsync(projectFilePath, cancellationToken);
        }
    }
}
