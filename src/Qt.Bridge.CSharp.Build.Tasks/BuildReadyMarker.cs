// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text;
using Qt.Bridge.Utils;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static class BuildReadyMarker
    {
        private const string Content = "{\"version\":1}";
        internal const string FileName = "qtbridge-build.ready";

        public static void Invalidate(string buildDirectory)
        {
            var markerPath = GetPath(buildDirectory);
            if (File.Exists(markerPath))
                File.Delete(markerPath);
        }

        public static string Publish(string buildDir, IReadOnlyCollection<string> importPaths)
        {
            if (buildDir == null)
                throw new ArgumentNullException(nameof(buildDir));
            if (importPaths == null)
                throw new ArgumentNullException(nameof(importPaths));

            ValidateGeneratedQmlTypes(importPaths);

            var markerPath = GetPath(buildDir);
            var qtDirectory = Path.GetDirectoryName(markerPath)
                ?? throw new InvalidOperationException("The marker path has no parent directory.");
            Directory.CreateDirectory(qtDirectory);
            File.WriteAllText(markerPath, Content, new UTF8Encoding(false));
            File.SetLastWriteTimeUtc(markerPath, DateTime.UtcNow);
            return markerPath;
        }

        private static string GetPath(string buildDirectory)
        {
            if (buildDirectory == null)
                throw new ArgumentNullException(nameof(buildDirectory));
            return Path.Combine(buildDirectory, ".qt", FileName);
        }

        private static void ValidateGeneratedQmlTypes(IEnumerable<string> importPaths)
        {
            foreach (var importPath in importPaths) {
                var hostPath = PathUtilities.ToHostSeparators(importPath);
                if (!Directory.Exists(hostPath)) {
                    throw new InvalidOperationException("Generated QML import directory "
                        + $"'{importPath}' does not exist.");
                }

                var qmlTypesFiles = Directory
                    .EnumerateFiles(hostPath, "*.qmltypes", SearchOption.TopDirectoryOnly)
                    .ToArray();
                if (qmlTypesFiles.Length == 0) {
                    throw new InvalidOperationException("Generated QML import directory "
                        + $"'{importPath}' contains no .qmltypes files.");
                }
            }
        }
    }
}
