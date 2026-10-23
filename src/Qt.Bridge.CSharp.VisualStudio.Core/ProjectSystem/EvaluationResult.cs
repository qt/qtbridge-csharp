// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Result of <see cref="IQtBridgeProjectCapabilityDetector.EvaluateAsync"/>.
    /// </summary>
    public sealed class EvaluationResult(bool hasCapability, string? packageId)
    {
        /// <summary> Neither the capability nor a Qt Bridge package reference. </summary>
        public static readonly EvaluationResult None = new(false, null);

        /// <summary>
        /// True once the project is restored and declares
        /// <see cref="QtBridgeProjectConstants.ProjectCapability"/>.
        /// </summary>
        public bool HasCapability { get; } = hasCapability;

        /// <summary>
        /// The Qt Bridge package that the active configuration references, or <c>null</c>. Known
        /// before a restore.
        /// </summary>
        public string? PackageId { get; } = packageId;

        public bool IsQtBridgeProject => HasCapability || PackageId != null;
    }
}
