// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Resolves Qt Bridge project metadata for an arbitrary file or project path.
    /// </summary>
    public interface IQtBridgeProjectService
    {
        /// <summary>
        /// Returns <see cref="QtBridgeProjectMetadata"/> for the project that owns
        /// <paramref name="path"/>, or <see langword="null"/> if no project file is found.
        /// </summary>
        Task<QtBridgeProjectMetadata?> TryGetMetadataForPathAsync(
            string path,
            CancellationToken cancellationToken = default);
    }
}
