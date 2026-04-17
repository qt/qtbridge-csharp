// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary> Analyzes a project file to determine whether it is a Qt Bridge project. </summary>
    public interface IQtBridgeProjectDetector
    {
        /// <summary>
        /// Reads and analyzes <paramref name="projectFilePath"/> and returns the detection result.
        /// </summary>
        Task<QtBridgeProjectMetadata> DetectAsync(
            string projectFilePath,
            CancellationToken cancellationToken = default);
    }
}
