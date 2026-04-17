// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Holds the static detection result for a single <c>.csproj</c> file, including which
    /// Qt Bridge indicators were found during analysis.
    /// </summary>
    public sealed class QtBridgeProjectMetadata(
        string projectFilePath,
        QtBridgeProjectType type,
        bool isQtBridgeProject,
        string? matchedPackageId,
        bool importsQtBridgeProps,
        bool importsQtBridgeTargets,
        IReadOnlyDictionary<string, string> properties)
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
    }
}
