// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text;
using Qt.Bridge.Utils;

using static Qt.Bridge.CSharp.Build.Tasks.QmlBuildMetadataIniUtilities;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    using WorkspaceEntries = Dictionary<int, Dictionary<string, string>>;

    internal static partial class QmllsBuildIniPatcher
    {
        internal const string FileName = ".qmlls.build.ini";

        internal enum IniFormat
        {
            Unknown,
            LegacySections,
            Workspaces
        }

        public static PatchResult Patch(
            string iniPath,
            string generatedSourceDir,
            string projectSourceDir,
            IReadOnlyCollection<string> fallbackImportPaths,
            IReadOnlyCollection<string> fallbackResourceFiles,
            string? projectSourcesQrcPath)
        {
            if (iniPath == null)
                throw new ArgumentNullException(nameof(iniPath));
            if (generatedSourceDir == null)
                throw new ArgumentNullException(nameof(generatedSourceDir));
            if (projectSourceDir == null)
                throw new ArgumentNullException(nameof(projectSourceDir));
            if (fallbackImportPaths == null)
                throw new ArgumentNullException(nameof(fallbackImportPaths));
            if (fallbackResourceFiles == null)
                throw new ArgumentNullException(nameof(fallbackResourceFiles));

            if (SameIniPath(generatedSourceDir, projectSourceDir))
                return new PatchResult(true, false, IniFormat.Unknown);

            if (!File.Exists(iniPath))
                return new PatchResult(false, false, IniFormat.Unknown);

            var lines = File.ReadAllLines(iniPath);
            var workspaceResult = TryPatchWorkspaces(
                lines,
                generatedSourceDir,
                projectSourceDir,
                projectSourcesQrcPath);
            if (workspaceResult.IsReady) {
                WriteIfChanged(iniPath, workspaceResult.Lines, workspaceResult.Changed);
                return new PatchResult(true, workspaceResult.Changed, IniFormat.Workspaces);
            }
            if (workspaceResult.Recognized)
                return new PatchResult(false, false, IniFormat.Workspaces);

            var legacyResult = TryPatchLegacySections(
                lines,
                generatedSourceDir,
                projectSourceDir,
                fallbackImportPaths,
                fallbackResourceFiles);
            if (!legacyResult.IsReady)
                return new PatchResult(false, false, IniFormat.Unknown);

            WriteIfChanged(iniPath, legacyResult.Lines, legacyResult.Changed);
            return new PatchResult(true, legacyResult.Changed, IniFormat.LegacySections);
        }

        private static FormatPatchResult TryPatchWorkspaces(
            string[] lines,
            string generatedSourceDir,
            string projectSourceDir,
            string? projectSourcesQrcPath)
        {
            var sectionStart = Array.FindIndex(lines, line =>
                string.Equals(line.Trim(), "[workspaces]", StringComparison.OrdinalIgnoreCase));
            if (sectionStart < 0)
                return FormatPatchResult.NotRecognized(lines);

            var sectionEnd = FindSectionEnd(lines, sectionStart);
            var (entries, sizeIndex, sizeValue) =
                ParseWorkspaceSection(lines, sectionStart, sectionEnd);
            var generatedIndex = FindWorkspaceByPath(entries, generatedSourceDir);
            if (generatedIndex == null)
                return FormatPatchResult.NotReady(lines);

            var generated = entries[generatedIndex.Value];
            var importPaths = generated.TryGetValue("importPaths", out var imports) ? imports : "";
            var resourceFiles = AddPath(
                generated.TryGetValue("resourceFiles", out var resources) ? resources : "",
                projectSourcesQrcPath);
            var updatedLines = lines.ToList();
            var aliasIndex = FindWorkspaceByPath(entries, projectSourceDir);

            if (aliasIndex != null) {
                SetWorkspaceValue(updatedLines, sectionStart, sectionEnd,
                    aliasIndex.Value, "importPaths", importPaths);
                SetWorkspaceValue(updatedLines, sectionStart, sectionEnd,
                    aliasIndex.Value, "resourceFiles", resourceFiles);
                return FormatPatchResult.Ready(lines, updatedLines);
            }

            var index = Math.Max(sizeValue + 1, entries.Count == 0 ? 1 : entries.Keys.Max() + 1);
            var insertAt = sizeIndex >= 0 ? sizeIndex : sectionEnd;
            var aliasLines = new[]
            {
                $"{index}\\sourcePath=\"{PathUtilities.ToForwardSlashes(projectSourceDir)}\"",
                $"{index}\\importPaths=\"{importPaths}\"",
                $"{index}\\resourceFiles=\"{resourceFiles}\""
            };
            updatedLines.InsertRange(insertAt, aliasLines);
            if (sizeIndex >= 0)
                updatedLines[sizeIndex + aliasLines.Length] = $"size={Math.Max(sizeValue, index)}";
            else
                updatedLines.Insert(insertAt + aliasLines.Length, $"size={index}");

            return FormatPatchResult.Ready(lines, updatedLines);
        }

        private static FormatPatchResult TryPatchLegacySections(
            string[] lines,
            string generatedSourceDir,
            string projectSourceDir,
            IReadOnlyCollection<string> importPaths,
            IReadOnlyCollection<string> resourceFiles)
        {
            var generatedKey = BuildSectionKey(generatedSourceDir);
            var aliasKey = BuildSectionKey(projectSourceDir);
            var aliasStart = Array.FindIndex(lines, line =>
                SameSectionKey(line, aliasKey, projectSourceDir));

            if (aliasStart >= 0) {
                var aliasEnd = FindSectionEnd(lines, aliasStart);
                var aliasLines = lines
                    .Skip(aliasStart + 1)
                    .Take(aliasEnd - aliasStart - 1)
                    .ToArray();
                var merged = MergeLegacySectionValues(
                    aliasLines,
                    importPaths,
                    resourceFiles).ToArray();
                var updatedLines = lines
                    .Take(aliasStart + 1)
                    .Concat(merged)
                    .Concat(lines.Skip(aliasEnd))
                    .ToList();
                return FormatPatchResult.Ready(lines, updatedLines);
            }

            var generatedStart = Array.FindIndex(lines, line =>
                SameSectionKey(line, generatedKey, generatedSourceDir));
            if (generatedStart < 0)
                return FormatPatchResult.NotReady(lines);

            var generatedEnd = FindSectionEnd(lines, generatedStart);
            var generatedLines = lines
                .Skip(generatedStart + 1)
                .Take(generatedEnd - generatedStart - 1)
                .Where(line => line.Trim().Length > 0)
                .ToArray();
            if (generatedLines.Length == 0)
                return FormatPatchResult.NotReady(lines);

            var updated = lines.ToList();
            if (updated.Count > 0 && updated[updated.Count - 1].Length > 0)
                updated.Add("");
            updated.Add(aliasKey);
            updated.AddRange(MergeLegacySectionValues(
                generatedLines,
                importPaths,
                resourceFiles));
            return FormatPatchResult.Ready(lines, updated);
        }

        private static (WorkspaceEntries, int, int) ParseWorkspaceSection(
            IReadOnlyList<string> lines,
            int sectionStart,
            int sectionEnd)
        {
            var entries = ParseWorkspaceEntries(lines, sectionStart, sectionEnd);

            var sizeIndex = -1;
            var sizeValue = 0;
            for (var i = sectionStart + 1; i < sectionEnd; ++i) {
                var line = lines[i].Trim();
                if (!line.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
                    continue;
                sizeIndex = i;
                _ = int.TryParse(line.Substring("size=".Length), out sizeValue);
            }

            return (entries, sizeIndex, sizeValue);
        }

        private static int? FindWorkspaceByPath(WorkspaceEntries entries, string path)
        {
            return entries
                .Where(entry => entry.Value.TryGetValue("sourcePath", out var sourcePath)
                    && SameIniPath(sourcePath, path))
                .Select(entry => (int?)entry.Key)
                .FirstOrDefault();
        }

        private static void SetWorkspaceValue(
            IList<string> lines,
            int sectionStart,
            int sectionEnd,
            int index,
            string key,
            string value)
        {
            for (var i = sectionStart + 1; i < sectionEnd && i < lines.Count; ++i) {
                if (!TryParseWorkspaceKey(lines[i].Trim(), out var lineIndex, out var lineKey,
                        out _)
                    || lineIndex != index
                    || !string.Equals(lineKey, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                var separator = lines[i].Contains('\\') ? '\\' : '/';
                lines[i] = $"{index}{separator}{key}=\"{value}\"";
                return;
            }

            lines.Insert(Math.Min(sectionEnd, lines.Count), $"{index}\\{key}=\"{value}\"");
        }

        private static IEnumerable<string> MergeLegacySectionValues(
            IEnumerable<string> sectionLines,
            IReadOnlyCollection<string> importPaths,
            IReadOnlyCollection<string> resourceFiles)
        {
            var result = sectionLines.ToList();
            MergePathValue(result, "importPaths", importPaths);
            MergePathValue(result, "resourceFiles", resourceFiles);
            return result;
        }

        private static void MergePathValue(
            List<string> lines,
            string key,
            IReadOnlyCollection<string> paths)
        {
            if (paths.Count == 0)
                return;

            var prefix = key + "=";
            var index = lines.FindIndex(line =>
                line.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (index < 0) {
                lines.Add($"{key}=\"{JoinPaths(paths)}\"");
                return;
            }

            lines[index] = AddPaths(lines[index], paths);
        }

        private static string AddPaths(string line, IEnumerable<string> paths)
        {
            var equals = line.IndexOf('=');
            if (equals < 0)
                return line;

            var value = line.Substring(equals + 1).Trim();
            var quoted = value.Length >= 2
                && value[0] == '"'
                && value[value.Length - 1] == '"';
            if (quoted)
                value = value.Substring(1, value.Length - 2);

            foreach (var path in paths) {
                if (!ContainsPath(value, path)) {
                    value = string.IsNullOrEmpty(value)
                        ? PathUtilities.ToForwardSlashes(path)
                        : value + ";" + PathUtilities.ToForwardSlashes(path);
                }
            }

            return line.Substring(0, equals + 1) + (quoted ? $"\"{value}\"" : value);
        }

        private static string AddPath(string paths, string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || ContainsPath(paths, path!))
                return paths;
            return string.IsNullOrEmpty(paths)
                ? PathUtilities.ToForwardSlashes(path!)
                : paths + ";" + PathUtilities.ToForwardSlashes(path!);
        }

        private static bool SameSectionKey(string line, string key, string path) =>
            string.Equals(line.Trim(), key, SectionComparison(path));

        private static string JoinPaths(IEnumerable<string> paths) =>
            string.Join(";", paths.Select(PathUtilities.ToForwardSlashes));

        private static void WriteIfChanged(string iniPath, IEnumerable<string> updated, bool changed)
        {
            if (!changed)
                return;

            var directory = Path.GetDirectoryName(iniPath)
                ?? throw new InvalidOperationException("The INI path has no parent directory.");

            var tempPath = Path.Combine(directory,
                "." + Path.GetFileName(iniPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try {
                File.WriteAllLines(tempPath, updated, new UTF8Encoding(false));
                File.Replace(tempPath, iniPath, null);
            } finally {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}
