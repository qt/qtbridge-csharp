// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Holds the detection result for a single <c>.csproj</c> file, including which Qt Bridge
    /// indicators were found during analysis.
    /// </summary>
    public sealed class QtBridgeProjectMetadata(
        string projectFilePath,
        QtBridgeProjectType type,
        bool isQtBridgeProject,
        string? matchedPackageId,
        bool importsQtBridgeProps,
        bool importsQtBridgeTargets,
        IReadOnlyDictionary<string, string> properties,
        bool hasQtBridgeCapability = false,
        bool hasEvaluatedPackageReference = false)
    {
        public string ProjectFilePath { get; } = projectFilePath
            ?? throw new ArgumentNullException(nameof(projectFilePath));
        public QtBridgeProjectType ProjectType { get; } = type;
        public bool IsQtBridgeProject { get; } = isQtBridgeProject;
        public string? MatchedPackageId { get; } = matchedPackageId;
        public bool ImportsQtBridgeProps { get; } = importsQtBridgeProps;
        public bool ImportsQtBridgeTargets { get; } = importsQtBridgeTargets;
        public IReadOnlyDictionary<string, string> Properties { get; } = properties
            ?? throw new ArgumentNullException(nameof(properties));
        public bool HasQtBridgeCapability { get; } = hasQtBridgeCapability;
        public bool HasEvaluatedPackageReference { get; } = hasEvaluatedPackageReference;

        /// <summary> Returns a copy that marks the project as detected by evaluation. </summary>
        public QtBridgeProjectMetadata WithEvaluation(EvaluationResult result) => new(
            ProjectFilePath,
            QtBridgeProjectType.QtBridgeCSharp,
            isQtBridgeProject: true,
            result.PackageId ?? MatchedPackageId,
            ImportsQtBridgeProps,
            ImportsQtBridgeTargets,
            Properties,
            result.HasCapability,
            hasEvaluatedPackageReference: result.PackageId != null);
    }
}
