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
                    return new WriteResult(null, false);
                File.Delete(qrcPath);
                return new WriteResult(null, true);
            }

            var content = BuildContent(qrcPath, fileInfos);

            Directory.CreateDirectory(qtDirectory);
            if (File.Exists(qrcPath)) {
                if (string.Equals(File.ReadAllText(qrcPath), content, StringComparison.Ordinal))
                    return new WriteResult(qrcPath, false);
            }

            File.WriteAllText(qrcPath, content, new UTF8Encoding(false));
            return new WriteResult(qrcPath, true);
        }

        private static string BuildContent(string qrcPath, IEnumerable<QmlFileInfo> qmlFileInfos)
        {
            var qrcDirectory = Path.GetDirectoryName(qrcPath)
                ?? throw new ArgumentException("QRC path must include a directory.", nameof(qrcPath));

            var content = new StringBuilder();
            content.AppendLine("<RCC>");
            foreach (var module in qmlFileInfos
                .OrderBy(file => file.ModulePath, StringComparer.Ordinal)
                .ThenBy(file => file.TypeName, StringComparer.Ordinal)
                .GroupBy(file => file.ModulePath, StringComparer.Ordinal)) {
                var prefix = "/qt/qml/" + PathUtilities.ToForwardSlashes(module.Key).Trim('/');
                content.Append("  <qresource prefix=\"");
                content.Append(XmlEscape(prefix));
                content.AppendLine("\">");
                foreach (var qmlFile in module) {
                    var sourcePath = PathUtilities.ToHostSeparators(qmlFile.SourcePath);
                    content.Append("    <file alias=\"");
                    content.Append(XmlEscape(Path.GetFileName(sourcePath)));
                    content.Append("\">");
                    content.Append(XmlEscape(MakeRelativePath(qrcDirectory, sourcePath)));
                    content.AppendLine("</file>");
                }
                content.AppendLine("  </qresource>");
            }
            content.AppendLine("</RCC>");
            return content.ToString();
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
