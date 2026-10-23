// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Queries the evaluated project for the Qt Bridge project capability and package reference.
    /// </summary>
    public interface IQtBridgeProjectCapabilityDetector
    {
        /// <summary>
        /// Returns whether the loaded project declares
        /// <see cref="QtBridgeProjectConstants.ProjectCapability"/> and which Qt Bridge package its
        /// active configuration references. The capability needs a restore, the package reference
        /// does not. Returns <see cref="EvaluationResult.None"/> if the project is not loaded.
        /// </summary>
        Task<EvaluationResult> EvaluateAsync(
            string projectFilePath,
            CancellationToken cancellationToken = default);
    }
}
