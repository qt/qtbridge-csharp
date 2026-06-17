// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

using CoreQmlMetadata = Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata.QmlMetadata;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    using V2WorkspaceEntries = Dictionary<int, Dictionary<string, string>>;

    /// <summary>
    /// Patches the <c>.qmlls.build.ini</c> file used by the QML Language Server to provide
    /// correct import paths, resource files, and workspace aliases for QML projects.
    ///
    /// This class ensures that the QML Language Server can resolve imports and resources
    /// at design time, even when the build directory structure differs from the source directory.
    /// </summary>
    internal sealed class QmllsBuildIniPatcher(IExtensionLog log)
    {
        private const string BuildIni = ".qmlls.build.ini";

        private enum FormatPatchResult
        {
            NotRecognized,
            Patched,
            NotReady
        }

        /// <summary>
        /// Attempts to patch the <c>.qmlls.build.ini</c> file for the given QML project.
        /// </summary>
        /// <remarks>
        /// Returns true if the ini file was found for at least one build dir (and was patched or
        /// already had the alias). Returns false if any required ini file is missing or could not
        /// be patched yet.
        /// </remarks>
        public bool TryPatch(CoreQmlMetadata metadata, string projectFilePath)
        {
            var projectSourceDir = Path.GetDirectoryName(projectFilePath)
                ?? metadata.Qml.ProjectSourceDir;
            if (string.IsNullOrEmpty(projectSourceDir)) {
                log.Info($"QML Language Server: {BuildIni} patch skipped - no projectSourceDir.");
                return true; // treat as "done" - nothing to patch without a project source dir
            }

            var generatedKey = BuildSectionKey(metadata.Qml.SourceDir);
            var aliasKey = BuildSectionKey(projectSourceDir!);

            log.Verbose($"QML Language Server: {BuildIni} patch - generatedKey={generatedKey}"
                + $" aliasKey={aliasKey}");

            if (string.Equals(generatedKey, aliasKey, StringComparison.OrdinalIgnoreCase)) {
                log.Verbose($"QML Language Server: {BuildIni} patch skipped - keys are equal.");
                return true;
            }

            var anyFound = false;
            var allReady = true;
            foreach (var buildDir in metadata.Qml.BuildDirs) {
                var iniPath = Path.Combine(buildDir, ".qt", BuildIni);
                if (!File.Exists(iniPath)) {
                    log.Verbose($"QML Language Server: {BuildIni} not found at '{iniPath}'.");
                    allReady = false;
                    continue;
                }
                anyFound = true;
                try {
                    var projectSourcesQrcPath = TryWriteProjectSourcesQrc(buildDir, metadata);
                    var resourceFiles = GetBuildResourceFiles(buildDir, projectSourcesQrcPath);
                    var v1ImportPaths = new[] { buildDir }
                        .Concat(metadata.Qml.ImportPaths)
                        .ToArray();
                    if (!PatchIniFile(
                        iniPath,
                        generatedKey,
                        aliasKey,
                        metadata.Qml.SourceDir,
                        projectSourceDir!,
                        v1ImportPaths,
                        projectSourcesQrcPath,
                        resourceFiles,
                        Path.GetFileNameWithoutExtension(projectFilePath))) {
                        allReady = false;
                    }
                } catch (Exception ex) when (ex is not OperationCanceledException) {
                    if (ex is IOException) {
                        log.Verbose($"QML Language Server: build settings file is not ready:"
                            + $" '{iniPath}' ({ex.Message}).");
                    } else {
                        log.Warning($"QML Language Server: failed to patch '{iniPath}':"
                            + $" {ex.Message}");
                    }
                    allReady = false;
                }
            }
            return anyFound && allReady;
        }

        /// <summary>
        /// Attempts to patch the INI file at the given path. Tries the v2 (workspaces) format
        /// first, then falls back to v1 (section-based) format.
        /// </summary>
        private bool PatchIniFile(
            string iniPath,
            string generatedKey,
            string aliasKey,
            string generatedSourceDir,
            string projectSourceDir,
            IReadOnlyCollection<string> importPaths,
            string? projectSourcesQrcPath,
            IReadOnlyCollection<string> resourceFiles,
            string projectName)
        {
            var lines = File.ReadAllLines(iniPath);
            var patchResult = TryPatchV2WorkspacesFormat(iniPath, lines, generatedSourceDir,
                projectSourceDir, projectSourcesQrcPath, projectName);

            switch (patchResult) {
            case FormatPatchResult.Patched:
                return true;
            case FormatPatchResult.NotReady:
                return false;
            case FormatPatchResult.NotRecognized:
            default:
                break;
            }

            log.Info($"QML Language Server: {BuildIni} format not recognized in '{iniPath}'"
                + $" ({lines.Length} lines). Trying v1 format.");
            return TryPatchV1SectionFormat(iniPath, lines, generatedKey, aliasKey,
                importPaths, resourceFiles, projectName);
        }

        /// <summary>
        /// Attempts to patch the INI file in the v1 (section-based) format.
        ///
        /// v1 format example:
        /// <code>
        /// [C:&lt;SLASH&gt;path&lt;SLASH&gt;to&lt;SLASH&gt;source]
        /// importPaths="..."
        /// resourceFiles="..."
        /// </code>
        /// </summary>
        private bool TryPatchV1SectionFormat(
            string iniPath,
            string[] lines,
            string generatedKey,
            string aliasKey,
            IReadOnlyCollection<string> importPaths,
            IReadOnlyCollection<string> resourceFiles,
            string projectName)
        {
            if (lines.Any(l => string.Equals(
                l.Trim(), aliasKey, StringComparison.OrdinalIgnoreCase))) {
                EnsureV1AliasSectionValues(iniPath, lines, aliasKey, importPaths, resourceFiles);
                log.Verbose($"QML Language Server: {BuildIni} already has alias '{aliasKey}'.");
                return true;
            }

            var generatedStart = Array.FindIndex(lines, l =>
                string.Equals(l.Trim(), generatedKey, StringComparison.OrdinalIgnoreCase));
            if (generatedStart < 0) {
                log.Verbose($"QML Language Server: {BuildIni} patch skipped - generated section"
                    + $" '{generatedKey}' not found in '{iniPath}' ({lines.Length} lines).");
                return false;
            }

            var generatedEnd = FindSectionEnd(lines, generatedStart);
            var sectionLines = lines
                .Skip(generatedStart + 1)
                .Take(generatedEnd - generatedStart - 1)
                .Where(l => l.Trim().Length > 0)
                .ToList();

            if (sectionLines.Count == 0) {
                log.Verbose($"QML Language Server: {BuildIni} patch skipped - generated section"
                    + $" '{generatedKey}' is empty in '{iniPath}'.");
                return false;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(aliasKey);
            foreach (var kv in MergeV1SectionValues(sectionLines, importPaths, resourceFiles))
                sb.AppendLine(kv);

            var existing = File.ReadAllText(iniPath);
            var toAppend = sb.ToString();
            if (existing.Length > 0 && existing[existing.Length - 1] != '\n')
                toAppend = Environment.NewLine + toAppend;

            File.AppendAllText(iniPath, toAppend);
            log.Info($"QML Language Server: patched {BuildIni} for '{projectName}'"
                + $" - added alias '{aliasKey}'.");
            return true;
        }

        /// <summary>
        /// Attempts to patch the INI file in the v2 (workspaces-based) format.
        ///
        /// v2 format example:
        /// <code>
        /// [workspaces]
        /// 1\sourcePath="..."
        /// 1\importPaths="..."
        /// 1\resourceFiles="..."
        /// size=1
        /// </code>
        /// </summary>
        private FormatPatchResult TryPatchV2WorkspacesFormat(
            string iniPath,
            string[] lines,
            string generatedSourceDir,
            string projectSourceDir,
            string? projectSourcesQrcPath,
            string projectName)
        {
            var workspacesStart = Array.FindIndex(lines, line =>
                string.Equals(line.Trim(), "[workspaces]", StringComparison.OrdinalIgnoreCase));
            if (workspacesStart < 0)
                return FormatPatchResult.NotRecognized;

            var workspacesEnd = FindSectionEnd(lines, workspacesStart);
            var (entries, sizeIndex, sizeValue) =
                ParseV2WorkspaceEntries(lines, workspacesStart, workspacesEnd);

            var generatedIndex = FindV2WorkspaceByPath(entries, generatedSourceDir);
            if (generatedIndex == null) {
                log.Verbose($"QML Language Server: {BuildIni} patch skipped - generated"
                    + $" workspace '{generatedSourceDir}' not found in '{iniPath}'.");
                return FormatPatchResult.NotReady;
            }

            var generated = entries[generatedIndex.Value];
            var importPaths = generated.TryGetValue("importPaths", out var imports) ? imports : "";
            var resourceFiles = AddResourcePath(
                generated.TryGetValue("resourceFiles", out var resources) ? resources : "",
                projectSourcesQrcPath);

            var updatedLines = lines.ToList();
            var aliasIndex = FindV2WorkspaceByPath(entries, projectSourceDir);

            if (aliasIndex != null) {
                SetV2WorkspaceValue(updatedLines, workspacesStart, workspacesEnd,
                    aliasIndex.Value, "importPaths", importPaths);
                SetV2WorkspaceValue(updatedLines, workspacesStart, workspacesEnd,
                    aliasIndex.Value, "resourceFiles", resourceFiles);
                if (!lines.SequenceEqual(updatedLines))
                    File.WriteAllLines(iniPath, updatedLines);
                log.Verbose($"QML Language Server: {BuildIni} already has workspace alias"
                    + $" '{projectSourceDir}'.");
                return FormatPatchResult.Patched;
            }

            var index = Math.Max(sizeValue + 1, entries.Count == 0 ? 1 : entries.Keys.Max() + 1);
            var insertAt = sizeIndex >= 0 ? sizeIndex : workspacesEnd;
            var aliasLines = new[]
            {
                $"{index}\\sourcePath=\"{Normalize(projectSourceDir)}\"",
                $"{index}\\importPaths=\"{importPaths}\"",
                $"{index}\\resourceFiles=\"{resourceFiles}\""
            };
            updatedLines.InsertRange(insertAt, aliasLines);
            if (sizeIndex >= 0)
                updatedLines[sizeIndex + aliasLines.Length] = $"size={Math.Max(sizeValue, index)}";
            else
                updatedLines.Insert(insertAt + aliasLines.Length, $"size={index}");

            File.WriteAllLines(iniPath, updatedLines);
            log.Info($"QML Language Server: patched {BuildIni} for '{projectName}'"
                + $" - added workspace alias '{projectSourceDir}'.");
            return FormatPatchResult.Patched;
        }

        /// <summary>
        /// Returns the line index of the next INI section header after
        /// <paramref name="sectionStart"/>, or <c>lines.Length</c> if no subsequent section exists.
        /// </summary>
        private static int FindSectionEnd(IReadOnlyList<string> lines, int sectionStart)
        {
            for (var i = sectionStart + 1; i < lines.Count; ++i) {
                var trimmed = lines[i].Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    return i;
            }
            return lines.Count;
        }

        /// <summary>
        /// Parses all workspace entries from the <c>[workspaces]</c> section of a v2 INI file.
        /// </summary>
        private static (V2WorkspaceEntries, int, int) ParseV2WorkspaceEntries(
            IReadOnlyList<string> lines,
            int workspacesStart,
            int workspacesEnd)
        {
            var entries = new V2WorkspaceEntries();
            var sizeIndex = -1;
            var sizeValue = 0;

            for (var i = workspacesStart + 1; i < workspacesEnd; ++i) {
                var line = lines[i].Trim();
                if (line.StartsWith("size=", StringComparison.OrdinalIgnoreCase)) {
                    sizeIndex = i;
                    _ = int.TryParse(line.Substring("size=".Length), out sizeValue);
                    continue;
                }

                var slash = line.IndexOf('\\');
                var equals = line.IndexOf('=');
                if (slash <= 0 || equals <= slash)
                    continue;
                if (!int.TryParse(line.Substring(0, slash), out var idx))
                    continue;

                var key = line.Substring(slash + 1, equals - slash - 1);
                var value = Unquote(line.Substring(equals + 1).Trim());
                if (!entries.TryGetValue(idx, out var values)) {
                    values = [];
                    entries[idx] = values;
                }
                values[key] = value;
            }

            return (entries, sizeIndex, sizeValue);
        }

        /// <summary>
        /// Returns the index of the first workspace entry whose <c>sourcePath</c> matches
        /// <paramref name="targetPath"/>, or <c>null</c> if not found.
        /// </summary>
        private static int? FindV2WorkspaceByPath(V2WorkspaceEntries entries, string targetPath)
        {
            return entries
                .Where(e =>
                    e.Value.TryGetValue("sourcePath", out var sp) && SamePath(sp, targetPath))
                .Select(e => (int?)e.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// Ensures that the alias section in a v1 INI file has the correct <c>importPaths</c> and
        /// <c>resourceFiles</c> values.
        /// </summary>
        private static void EnsureV1AliasSectionValues(
            string iniPath,
            string[] lines,
            string aliasKey,
            IReadOnlyCollection<string> importPaths,
            IReadOnlyCollection<string> resourceFiles)
        {
            if (importPaths.Count == 0 && resourceFiles.Count == 0)
                return;

            var start = Array.FindIndex(lines, l =>
                string.Equals(l.Trim(), aliasKey, StringComparison.OrdinalIgnoreCase));
            if (start < 0)
                return;

            var end = FindSectionEnd(lines, start);
            var sectionLines = lines.Skip(start + 1).Take(end - start - 1).ToArray();
            var merged = MergeV1SectionValues(sectionLines, importPaths, resourceFiles).ToArray();
            if (sectionLines.SequenceEqual(merged))
                return;

            var updated = lines.Take(start + 1)
                .Concat(merged)
                .Concat(lines.Skip(end));
            File.WriteAllLines(iniPath, updated);
        }

        /// <summary> Updates or inserts a value for a workspace entry in a v2 INI file. </summary>
        private static void SetV2WorkspaceValue(
            IList<string> lines,
            int workspacesStart,
            int workspacesEnd,
            int index,
            string key,
            string value)
        {
            var prefix = $"{index}\\{key}=";
            for (var i = workspacesStart + 1; i < workspacesEnd && i < lines.Count; ++i) {
                if (!lines[i].TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                lines[i] = prefix + "\"" + value + "\"";
                return;
            }

            var insertAt = workspacesEnd <= lines.Count ? workspacesEnd : lines.Count;
            lines.Insert(insertAt, prefix + "\"" + value + "\"");
        }

        /// <summary>
        /// Converts a file system path to a section key for the v1 INI format.
        /// Example:
        /// <c>C:/path/to/source -> [C:&lt;SLASH&gt;path&lt;SLASH&gt;to&lt;SLASH&gt;source]</c>.
        /// </summary>
        private static string BuildSectionKey(string path)
        {
            var normalized = Normalize(path).TrimEnd('/');
            if (normalized.Length >= 2 && normalized[1] == ':')
                normalized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
            return "[" + normalized.Replace("/", "<SLASH>") + "]";
        }

        /// <summary>
        /// Generates a <c>.qrc</c> file (Qt Resource Collection) for the project's QML files.
        /// The file is written to the build directory's <c>.qt</c> subdirectory.
        /// </summary>
        private static string? TryWriteProjectSourcesQrc(string buildDir, CoreQmlMetadata metadata)
        {
            if (metadata.Qml.Files.Count == 0)
                return null;

            var qtDir = Path.Combine(buildDir, ".qt");
            Directory.CreateDirectory(qtDir);
            var qrcPath = Path.Combine(qtDir, "qtbridge_project_sources.qrc");
            var qrcDir = Path.GetDirectoryName(qrcPath) ?? qtDir;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<RCC>");
            foreach (var module in metadata.Qml.Files
                .OrderBy(f => f.ModulePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.TypeName, StringComparer.OrdinalIgnoreCase)
                .GroupBy(f => f.ModulePath, StringComparer.OrdinalIgnoreCase)) {
                var prefix = "/qt/qml/" + Normalize(module.Key).Trim('/');
                sb.Append("  <qresource prefix=\"");
                sb.Append(XmlEscape(prefix));
                sb.AppendLine("\">");
                foreach (var qmlFile in module) {
                    sb.Append("    <file alias=\"");
                    sb.Append(XmlEscape(Path.GetFileName(qmlFile.SourcePath)));
                    sb.Append("\">");
                    sb.Append(XmlEscape(MakeQrcRelativePath(qrcDir, qmlFile.SourcePath)));
                    sb.AppendLine("</file>");
                }
                sb.AppendLine("  </qresource>");
            }
            sb.AppendLine("</RCC>");

            var content = sb.ToString();
            if (!File.Exists(qrcPath) || File.ReadAllText(qrcPath) != content)
                File.WriteAllText(qrcPath, content);

            return qrcPath;
        }

        /// <summary>
        /// Collects all <c>.qrc</c> files in the build directory and its subdirectories.
        /// </summary>
        private static string[] GetBuildResourceFiles(string buildDir, string? projectSourcesQrcPath)
        {
            var resourceFiles = Directory.Exists(buildDir)
                ? Directory.GetFiles(buildDir, "*.qrc", SearchOption.AllDirectories)
                    .Select(Normalize)
                    .ToList()
                : [];

            if (string.IsNullOrWhiteSpace(projectSourcesQrcPath))
                return [..resourceFiles];

            var qrcPath = Normalize(projectSourcesQrcPath!);
            if (!resourceFiles.Any(path =>
                string.Equals(path, qrcPath, StringComparison.OrdinalIgnoreCase))) {
                resourceFiles.Add(qrcPath);
            }

            return [..resourceFiles];
        }

        /// <summary>
        /// Merges <c>importPaths</c> and <c>resourceFiles</c> into a list of section lines for the
        /// v1 INI format.
        /// </summary>
        private static IEnumerable<string> MergeV1SectionValues(
            IEnumerable<string> sectionLines,
            IReadOnlyCollection<string> importPaths,
            IReadOnlyCollection<string> resourceFiles)
        {
            var result = sectionLines.ToList();

            if (importPaths.Count > 0) {
                var importIndex = IndexOf(result, "importPaths=");
                if (importIndex < 0)
                    result.Add($"importPaths=\"{JoinPaths(importPaths)}\"");
                else
                    result[importIndex] = AddImportPaths(result[importIndex], importPaths);
            }

            if (resourceFiles.Count == 0)
                return result;

            var resourceIndex = IndexOf(result, "resourceFiles=");
            if (resourceIndex < 0) {
                result.Add($"resourceFiles=\"{JoinPaths(resourceFiles)}\"");
                return result;
            }

            result[resourceIndex] = AddResourceFiles(result[resourceIndex], resourceFiles);
            return result;

            static int IndexOf(List<string> source, string value) => source.FindIndex(l =>
                l.TrimStart().StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds import paths to an existing <c>importPaths=</c> line in the INI file.
        /// </summary>
        private static string AddImportPaths(string line, IReadOnlyCollection<string> importPaths)
        {
            if (importPaths.Count == 0)
                return line;

            var equals = line.IndexOf('=');
            if (equals < 0)
                return line;

            var value = line.Substring(equals + 1).Trim();
            var quote = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
            if (quote)
                value = value.Substring(1, value.Length - 2);

            var normalizedValue = Normalize(value);
            foreach (var importPath in importPaths.Select(Normalize)) {
                if (ContainsPath(normalizedValue, importPath))
                    continue;
                value = string.IsNullOrEmpty(value) ? importPath : value + ";" + importPath;
                normalizedValue = string.IsNullOrEmpty(normalizedValue)
                    ? importPath
                    : normalizedValue + ";" + importPath;
            }

            return line.Substring(0, equals + 1) + (quote ? $"\"{value}\"" : value);
        }

        /// <summary>
        /// Joins a list of normalized paths into a semicolon-separated string.
        /// </summary>
        private static string JoinPaths(IEnumerable<string> paths)
        {
            return string.Join(";", paths.Select(Normalize));
        }

        /// <summary>
        /// Adds resource files to an existing <c>resourceFiles=</c> line in the INI file.
        /// </summary>
        private static string AddResourceFiles(string line, IReadOnlyCollection<string> resourceFiles)
        {
            if (resourceFiles.Count == 0)
                return line;

            var equals = line.IndexOf('=');
            if (equals < 0)
                return line;

            var value = line.Substring(equals + 1).Trim();
            var quote = value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"';
            if (quote)
                value = value.Substring(1, value.Length - 2);

            var normalizedValue = Normalize(value);
            foreach (var resourceFilePath in resourceFiles.Select(Normalize)) {
                if (ContainsPath(normalizedValue, resourceFilePath))
                    continue;
                value = string.IsNullOrEmpty(value)
                    ? resourceFilePath
                    : value + ";" + resourceFilePath;
                normalizedValue = string.IsNullOrEmpty(normalizedValue)
                    ? resourceFilePath
                    : normalizedValue + ";" + resourceFilePath;
            }
            return line.Substring(0, equals + 1) + (quote ? $"\"{value}\"" : value);
        }

        /// <summary>
        /// Adds a resource file path to an existing semicolon-separated list of resource files.
        /// </summary>
        private static string AddResourcePath(string resourceFiles, string? resourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(resourceFilePath))
                return resourceFiles;

            var qrcPath = Normalize(resourceFilePath!);
            if (ContainsPath(Normalize(resourceFiles), qrcPath))
                return resourceFiles;

            return string.IsNullOrEmpty(resourceFiles)
                ? qrcPath
                : resourceFiles + ";" + qrcPath;
        }

        /// <summary>
        /// Compares two file system paths for equality, normalizing separators and case.
        /// </summary>
        private static bool SamePath(string left, string right)
        {
            left = ResolveAndNormalize(Unquote(left)).TrimEnd('/');
            right = ResolveAndNormalize(Unquote(right)).TrimEnd('/');
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a semicolon-separated list of paths contains the given path.
        /// Both the list and the path to find are normalized to use forward slashes.
        /// </summary>
        private static bool ContainsPath(string pathList, string pathToFind)
        {
            var normalizedPathToFind = Normalize(pathToFind);
            return pathList.Split(';').Any(path => string.Equals(path, normalizedPathToFind,
                StringComparison.OrdinalIgnoreCase));
        }

        /// <summary> Removes surrounding quotes from a string if present. </summary>
        private static string Unquote(string value)
        {
            value = value.Trim();
            return value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"'
                ? value.Substring(1, value.Length - 2)
                : value;
        }

        /// <summary> Normalizes a path to use forward slashes. </summary>
        private static string Normalize(string path) => path.Replace('\\', '/');

        /// <summary>
        /// Resolves a path to an absolute path and normalizes it to use forward slashes.
        /// </summary>
        private static string ResolveAndNormalize(string path)
        {
            try {
                return Normalize(Path.GetFullPath(path));
            } catch {
                return Normalize(path);
            }
        }

        /// <summary>
        /// Converts an absolute file path to a relative path for use in a <c>.qrc</c> file.
        /// </summary>
        private static string MakeQrcRelativePath(string baseDir, string filePath)
        {
            try {
                var baseUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(baseDir)));
                var file = new Uri(Path.GetFullPath(filePath));
                return Normalize(Uri.UnescapeDataString(baseUri.MakeRelativeUri(file).ToString()));
            } catch {
                return Normalize(filePath);
            }
        }

        /// <summary> Ensures that a path ends with a directory separator. </summary>
        private static string AppendDirectorySeparator(string path)
        {
            if (path.Length <= 0)
                return path + Path.DirectorySeparatorChar;

            return path[path.Length - 1] switch
            {
                '\\' or '/' => path,
                _ => path + Path.DirectorySeparatorChar
            };
        }

        /// <summary> Escapes special characters in a string for use in an XML file. </summary>
        private static string XmlEscape(string value) =>
            System.Security.SecurityElement.Escape(value) ?? "";
    }
}
