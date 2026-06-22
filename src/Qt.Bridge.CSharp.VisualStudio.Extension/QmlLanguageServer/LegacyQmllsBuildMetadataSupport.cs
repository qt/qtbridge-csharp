// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;

using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

using CoreQmlMetadata = Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata.QmlMetadata;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    /// <summary>
    /// TODO: After dropping legacy producer support, remove this class.
    /// </summary>
    internal sealed class LegacyQmllsBuildMetadataSupport
    {
        private readonly IExtensionLog log;
        private readonly QmllsBuildIniPatcher buildIniPatcher;

        public LegacyQmllsBuildMetadataSupport(IExtensionLog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            buildIniPatcher = new QmllsBuildIniPatcher(this.log);
        }

        public string NotReadyReason { get; private set; } = ".qmlls.build.ini exists";

        public bool AreReady(CoreQmlMetadata metadata, string projectFilePath)
        {
            if (!BuildSettingsIniFilesExist(metadata))
                return NotReady(".qmlls.build.ini exists");

            if (!buildIniPatcher.TryPatch(metadata, projectFilePath))
                return NotReady(".qmlls.build.ini exists");

            if (!GeneratedQmlTypesReady(metadata, projectFilePath))
                return NotReady("generated .qmltypes files exist");

            return true;
        }

        public IEnumerable<string> GetWatchedPaths(CoreQmlMetadata metadata)
        {
            var buildDirs = metadata.Qml.BuildDirs
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var buildDir in buildDirs) {
                var dir = Path.Combine(buildDir, ".qt");
                yield return Path.Combine(dir, ".qmlls.build.ini");
                yield return Path.Combine(dir, "qtbridge_project_sources.qrc");
            }

            var generatedImportPaths = metadata.Qml.ImportPaths
                .Where(path => buildDirs.Any(buildDir => IsPathUnder(path, buildDir)))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var importPath in generatedImportPaths) {
                yield return importPath;
                if (!Directory.Exists(importPath))
                    continue;
                foreach (var qmlTypesPath in Directory.EnumerateFiles(
                    importPath, "*.qmltypes", SearchOption.TopDirectoryOnly)) {
                    yield return qmlTypesPath;
                }
            }
        }

        private bool GeneratedQmlTypesReady(CoreQmlMetadata metadata, string projectFilePath)
        {
            var buildDirs = metadata.Qml.BuildDirs
                .Select(Path.GetFullPath)
                .ToArray();
            var generatedImportPaths = metadata.Qml.ImportPaths
                .Where(path => buildDirs.Any(buildDir => IsPathUnder(path, buildDir)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (generatedImportPaths.Length == 0)
                return true;

            foreach (var importPath in generatedImportPaths) {
                if (!Directory.Exists(importPath)) {
                    log.Verbose($"QML Language Server: generated import path not found"
                        + $" for '{Path.GetFileName(projectFilePath)}': '{importPath}'.");
                    return false;
                }

                var qmlTypes = Directory.EnumerateFiles(
                    importPath, "*.qmltypes", SearchOption.TopDirectoryOnly).ToArray();
                if (qmlTypes.Length == 0) {
                    log.Verbose($"QML Language Server: no generated .qmltypes files found"
                        + $" for '{Path.GetFileName(projectFilePath)}' in '{importPath}'.");
                    return false;
                }

                foreach (var qmlTypesPath in qmlTypes) {
                    try {
                        using var _ = new FileStream(
                            qmlTypesPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read);
                    } catch (IOException ex) {
                        log.Verbose($"QML Language Server: generated .qmltypes file is not ready"
                            + $" for '{Path.GetFileName(projectFilePath)}':"
                            + $" '{qmlTypesPath}' ({ex.Message}).");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool NotReady(string reason)
        {
            NotReadyReason = reason;
            return false;
        }

        private static bool BuildSettingsIniFilesExist(CoreQmlMetadata metadata)
        {
            return metadata.Qml.BuildDirs
                .All(buildDir => File.Exists(Path.Combine(buildDir, ".qt", ".qmlls.build.ini")));
        }

        private static bool IsPathUnder(string path, string root)
        {
            var fullPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var fullRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
