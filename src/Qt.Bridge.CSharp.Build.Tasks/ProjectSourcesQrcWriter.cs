// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Security;
using System.Text;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static partial class ProjectSourcesQrcWriter
    {
        private const string FileName = "qtbridge_project_sources.qrc";

        public static WriteResult Write(string buildDir, IReadOnlyCollection<QmlFileInfo> fileInfos)
        {
            if (string.IsNullOrWhiteSpace(buildDir))
                throw new ArgumentException("Build directory is required.", nameof(buildDir));
            if (fileInfos == null)
                throw new ArgumentNullException(nameof(fileInfos));

            var qtDirectory = Path.Combine(buildDir, ".qt");
            var qrcPath = Path.Combine(qtDirectory, FileName);
            if (fileInfos.Count == 0) {
                if (!File.Exists(qrcPath))
                    return new WriteResult(null, false, []);
                File.Delete(qrcPath);
                return new WriteResult(null, true, []);
            }

            var buildContent = BuildContent(qrcPath, fileInfos);
            var content = buildContent.Content;

            Directory.CreateDirectory(qtDirectory);
            if (File.Exists(qrcPath)) {
                if (string.Equals(File.ReadAllText(qrcPath), content, StringComparison.Ordinal))
                    return new WriteResult(qrcPath, false, buildContent.Collisions);
            }

            File.WriteAllText(qrcPath, content, new UTF8Encoding(false));
            return new WriteResult(qrcPath, true, buildContent.Collisions);
        }

        private static BuildContentResult BuildContent(
            string qrcPath,
            IEnumerable<QmlFileInfo> qmlFileInfos)
        {
            var qrcDirectory = Path.GetDirectoryName(qrcPath) ??
                throw new ArgumentException("QRC path must include a directory.", nameof(qrcPath));

            var entries = qmlFileInfos
                .Select(file => new QmlFileQrcEntry(
                    file,
                    PathUtilities.ToForwardSlashes(file.ModulePath).Trim('/'),
                    PathUtilities.ToHostSeparators(file.SourcePath)))
                .OrderBy(file => file.ModulePath, StringComparer.Ordinal)
                .ThenBy(file => file.QmlFileInfo.TypeName, StringComparer.Ordinal)
                .ToArray();
            var (filteredEntries, collisions) = FilterResourceIdentityCollisions(entries);

            var content = new StringBuilder();
            content.AppendLine("<RCC>");
            foreach (var entry in filteredEntries.GroupBy(
                file => file.ModulePath,
                StringComparer.Ordinal)) {
                var prefix = "/qt/qml/" + entry.Key;
                content.Append("  <qresource prefix=\"");
                content.Append(XmlEscape(prefix));
                content.AppendLine("\">");
                foreach (var file in entry) {
                    content.Append("    <file alias=\"");
                    content.Append(XmlEscape(file.Alias));
                    content.Append("\">");
                    content.Append(XmlEscape(MakeRelativePath(qrcDirectory, file.SourcePath)));
                    content.AppendLine("</file>");
                }
                content.AppendLine("  </qresource>");
            }
            content.AppendLine("</RCC>");
            return new BuildContentResult(content.ToString(), collisions);
        }

        private static (QmlFileQrcEntry[] FilteredEntries, ResourceIdentityCollision[] Collisions)
            FilterResourceIdentityCollisions(IEnumerable<QmlFileQrcEntry> files)
        {
            var filtered = new List<QmlFileQrcEntry>();
            var collisions = new List<ResourceIdentityCollision>();

            foreach (var group in files
                .GroupBy(file => file.ModulePath + "\0" + file.Alias, StringComparer.Ordinal)
                .Select(group => group.ToArray())) {
                filtered.Add(group[0]);
                if (group.Length == 1)
                    continue;

                var first = group[0];
                var resourcePath = "/qt/qml/" + first.ModulePath + "/" + first.Alias;
                collisions.Add(new ResourceIdentityCollision(
                    resourcePath,
                    group.Select(f => PathUtilities.ToForwardSlashes(f.QmlFileInfo.SourcePath))
                        .ToArray()));
            }

            return (filtered.ToArray(), collisions.ToArray());
        }

        private static string MakeRelativePath(string baseDirectory, string filePath)
        {
            try {
                var basePath = new Uri(AppendDirectorySeparator(Path.GetFullPath(baseDirectory)));
                var fileUri = new Uri(Path.GetFullPath(filePath));
                return PathUtilities.ToForwardSlashes(
                    Uri.UnescapeDataString(basePath.MakeRelativeUri(fileUri).ToString()));
            } catch {
                return PathUtilities.ToForwardSlashes(filePath);
            }
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.Length > 0 && path[path.Length - 1] is '\\' or '/'
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? "";
    }
}
