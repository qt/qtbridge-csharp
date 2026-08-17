// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.Utils;
using static Qt.Bridge.CSharp.Build.Tasks.QmlBuildMetadataIniUtilities;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static class QmlBuildMetadataValidator
    {
        public static string? Validate(
            string buildIniPath,
            string projectSourceDirectory,
            string? projectSourcesQrcPath)
        {
            if (string.IsNullOrWhiteSpace(buildIniPath))
                throw new ArgumentException("Build INI path is required.", nameof(buildIniPath));
            if (string.IsNullOrWhiteSpace(projectSourceDirectory)) {
                throw new ArgumentException("Project source directory is required.",
                    nameof(projectSourceDirectory));
            }

            if (!File.Exists(buildIniPath))
                return $"'{buildIniPath}' does not exist.";

            if (!string.IsNullOrWhiteSpace(projectSourcesQrcPath)) {
                if (!File.Exists(projectSourcesQrcPath))
                    return $"'{projectSourcesQrcPath}' does not exist.";
            }

            var lines = File.ReadAllLines(buildIniPath);
            var aliasValidationError = ValidateWorkspaceAlias(
                lines,
                projectSourceDirectory,
                projectSourcesQrcPath);
            return aliasValidationError;
        }

        private static string? ValidateWorkspaceAlias(
            IReadOnlyList<string> lines,
            string projectSourceDirectory,
            string? projectSourcesQrcPath)
        {
            var workspaceSectionStart = Array.FindIndex(lines.ToArray(), line =>
                string.Equals(line.Trim(), "[workspaces]", StringComparison.OrdinalIgnoreCase));
            if (workspaceSectionStart >= 0) {
                return ValidateWorkspaces(
                    lines,
                    workspaceSectionStart,
                    projectSourceDirectory,
                    projectSourcesQrcPath);
            }

            return ValidateLegacySections(lines, projectSourceDirectory, projectSourcesQrcPath);
        }

        private static string? ValidateWorkspaces(
            IReadOnlyList<string> lines,
            int workspaceSectionStart,
            string projectSourceDirectory,
            string? projectSourcesQrcPath)
        {
            var workspaceSectionEnd = FindSectionEnd(lines, workspaceSectionStart);
            var entries = ParseWorkspaceEntries(lines, workspaceSectionStart, workspaceSectionEnd);
            var aliasEntry = entries.FirstOrDefault(entry =>
                entry.Value.TryGetValue("sourcePath", out var sourcePath)
                && PathUtilities.AreEquivalent(sourcePath, projectSourceDirectory));
            if (aliasEntry.Value == null) {
                return $"'{PathUtilities.ToForwardSlashes(projectSourceDirectory)}' alias is "
                    + $"missing from '{lines[workspaceSectionStart]}'.";
            }

            if (string.IsNullOrWhiteSpace(projectSourcesQrcPath))
                return null;

            var resourceFiles = aliasEntry.Value.TryGetValue("resourceFiles", out var value);
            if (ContainsPath(resourceFiles ? value ?? "" : "", projectSourcesQrcPath!))
                return null;

            return $"'{PathUtilities.ToForwardSlashes(projectSourceDirectory)}' alias does not "
                + $"reference '{PathUtilities.ToForwardSlashes(projectSourcesQrcPath!)}'.";
        }

        private static string? ValidateLegacySections(
            IReadOnlyList<string> lines,
            string projectSourceDirectory,
            string? projectSourcesQrcPath)
        {
            var aliasKey = BuildSectionKey(projectSourceDirectory);
            var aliasStart = Array.FindIndex(lines.ToArray(), line =>
                string.Equals(line.Trim(), aliasKey, SectionComparison(projectSourceDirectory)));
            if (aliasStart < 0) {
                return $"'{PathUtilities.ToForwardSlashes(projectSourceDirectory)}' alias section "
                    + "is missing.";
            }

            if (string.IsNullOrWhiteSpace(projectSourcesQrcPath))
                return null;

            var aliasEnd = FindSectionEnd(lines, aliasStart);
            var resourceFilesLine = lines
                .Skip(aliasStart + 1)
                .Take(aliasEnd - aliasStart - 1)
                .FirstOrDefault(line => line.TrimStart().StartsWith(
                    "resourceFiles=",
                    StringComparison.OrdinalIgnoreCase));
            if (resourceFilesLine == null) {
                return $"'{PathUtilities.ToForwardSlashes(projectSourceDirectory)}' alias does not "
                    + "define resourceFiles.";
            }

            var value = resourceFilesLine.Substring(resourceFilesLine.IndexOf('=') + 1).Trim()
                .Trim('"');
            if (ContainsPath(value, projectSourcesQrcPath!))
                return null;
            return $"'{PathUtilities.ToForwardSlashes(projectSourceDirectory)}' alias does not "
                + $"reference '{PathUtilities.ToForwardSlashes(projectSourcesQrcPath!)}'.";
        }
    }
}
