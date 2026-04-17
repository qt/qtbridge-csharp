// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Text;

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Formats <see cref="QtBridgeProjectMetadata"/> as a human-readable diagnostic string.
    /// </summary>
    public static class QtBridgeProjectSummaryFormatter
    {
        /// <summary>
        /// Returns a multi-line diagnostic summary of <paramref name="metadata"/>.
        /// </summary>
        public static string Format(QtBridgeProjectMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var builder = new StringBuilder();
            builder.AppendLine($"Project: {metadata.ProjectFilePath}");
            builder.AppendLine($"Detected: {metadata.IsQtBridgeProject}");
            builder.AppendLine($"Project type: {metadata.ProjectType}");
            builder.AppendLine($"Package: {metadata.MatchedPackageId ?? "<none>"}");
            builder.AppendLine($"Imports props: {metadata.ImportsQtBridgeProps}");
            builder.AppendLine($"Imports targets: {metadata.ImportsQtBridgeTargets}");

            foreach (var property in metadata.Properties)
                builder.AppendLine($"{property.Key}: {property.Value}");

            return builder.ToString().TrimEnd();
        }
    }
}
