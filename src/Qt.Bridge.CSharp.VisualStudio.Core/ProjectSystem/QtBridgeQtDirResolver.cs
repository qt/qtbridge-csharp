// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text.RegularExpressions;

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Resolves the Qt installation a Qt Bridge project builds against. The order it tries to
    /// resolve the installation:
    /// 1) the project's <c>QtDir</c> property
    /// 2) the <c>QTDIR</c> environment variable
    /// </summary>
    public static class QtBridgeQtDirResolver
    {
        private const string ProjectPropertyName = "QtDir";

        private const string EnvironmentVariableName = "QTDIR";

        private static readonly Regex PropertyReference = new(@"\$\(([A-Za-z_][\w.-]*)\)");

        public static string? Resolve(QtBridgeProjectMetadata project) =>
            Resolve(project, Environment.GetEnvironmentVariable);

        /// <summary>
        /// Returns the Qt installation directory, or <see langword="null"/> if none is configured
        /// or if a configured value refers to a property that cannot be resolved.
        /// </summary>
        public static string? Resolve(
            QtBridgeProjectMetadata project,
            Func<string, string?> getEnvironmentVariable)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (getEnvironmentVariable == null)
                throw new ArgumentNullException(nameof(getEnvironmentVariable));

            if (project.Properties.TryGetValue(ProjectPropertyName, out var value)) {
                var expanded = Expand(value, project.Properties, getEnvironmentVariable);
                if (expanded != null)
                    return expanded;
            }

            var environmentValue = getEnvironmentVariable(EnvironmentVariableName)?.Trim();
            return string.IsNullOrEmpty(environmentValue) ? null : environmentValue;
        }

        private static string? Expand(
            string value,
            IReadOnlyDictionary<string, string> properties,
            Func<string, string?> getEnvironmentVariable)
        {
            var current = value.Trim();
            for (var i = 0; i < 8; ++i) {
                var next = PropertyReference.Replace(current, match =>
                {
                    var name = match.Groups[1].Value;
                    return properties.TryGetValue(name, out var property)
                        ? property
                        : getEnvironmentVariable(name) ?? match.Value;
                });
                if (next == current)
                    break;
                current = next;
            }

            current = current.Trim();
            return current.Length == 0 || current.Contains("$(") ? null : current;
        }
    }
}
