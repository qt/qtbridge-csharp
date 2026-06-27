// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Microsoft.Build.Framework;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    public sealed class PrepareQmlBuildMetadata : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string BuildDirectory { get; set; } = "";

        [Required]
        public string GeneratedSourceDirectory { get; set; } = "";

        [Required]
        public string ProjectSourceDirectory { get; set; } = "";

        public ITaskItem[] QmlFiles { get; set; } = [];

        public ITaskItem[] ImportPaths { get; set; } = [];

        public ITaskItem[] ResourceFiles { get; set; } = [];

        public ITaskItem[] GeneratedImportPaths { get; set; } = [];

        [Output]
        public string BuildIniPath { get; set; } = "";

        [Output]
        public string? ProjectSourcesQrcPath { get; set; }

        [Output]
        public bool BuildIniChanged { get; set; }

        [Output]
        public bool ProjectSourcesQrcChanged { get; set; }

        [Output]
        public string ReadyMarkerPath { get; set; } = "";

        public override bool Execute()
        {
            try {
                if (!ValidateInputs())
                    return false;

                BuildReadyMarker.Invalidate(BuildDirectory);
                BuildIniPath = Path.Combine(BuildDirectory, ".qt", QmllsBuildIniPatcher.FileName);
                if (!File.Exists(BuildIniPath)) {
                    Log.LogWarning("Qt Bridge could not prepare QML Language Server metadata "
                        + $"because '{BuildIniPath}' does not exist.");
                    return false;
                }

                var (qmlFiles, qmlFilesValid) = CreateQmlFileInfos();
                if (!qmlFilesValid)
                    return false;

                var qrcWriteResult = ProjectSourcesQrcWriter.Write(BuildDirectory, qmlFiles);
                ProjectSourcesQrcPath = qrcWriteResult.Path;
                ProjectSourcesQrcChanged = qrcWriteResult.Changed;
                foreach (var collision in qrcWriteResult.Collisions) {
                    var paths = string.Join("', '", collision.SourcePaths);
                    Log.LogWarning("Qt Bridge omitted colliding QML files from qmlls metadata: "
                        + $"'{paths}' map to the same resource path '{collision.ResourcePath}'.");
                }

                var importPaths = PrependDistinctPath(
                    BuildDirectory,
                    ImportPaths.Select(item => item.ItemSpec));
                var resourceFiles = AppendDistinctPath(
                    ResourceFiles.Select(item => item.ItemSpec),
                    ProjectSourcesQrcPath);

                Log.LogMessage(MessageImportance.High, $"Patching .qmlls.build.ini at {BuildIniPath}");

                var patchResult = QmllsBuildIniPatcher.Patch(
                    BuildIniPath,
                    GeneratedSourceDirectory,
                    ProjectSourceDirectory,
                    importPaths,
                    resourceFiles,
                    ProjectSourcesQrcPath);
                BuildIniChanged = patchResult.Changed;
                if (!patchResult.IsReady) {
                    Log.LogWarning("Qt Bridge could not prepare QML Language Server metadata "
                        + $"because the generated workspace '{GeneratedSourceDirectory}' was "
                        + $"not found in '{BuildIniPath}'.");
                    return false;
                }

                var validationError = QmlBuildMetadataValidator.Validate(
                    BuildIniPath,
                    ProjectSourceDirectory,
                    ProjectSourcesQrcPath);
                if (validationError != null) {
                    Log.LogWarning("Qt Bridge could not publish QML Language Server metadata "
                        + $"because {validationError}");
                    return false;
                }

                var markerPath = Path.Combine(BuildDirectory, ".qt", BuildReadyMarker.FileName);
                Log.LogMessage(MessageImportance.High, $"Populating {markerPath} marker");

                ReadyMarkerPath = BuildReadyMarker.Publish(BuildDirectory,
                    GeneratedImportPaths.Select(item => item.ItemSpec).ToArray());
                return true;
            } catch (Exception exception) {
                Log.LogWarningFromException(exception, showStackTrace: false);
                return false;
            }
        }

        private bool ValidateInputs()
        {
            var valid = true;
            if (string.IsNullOrWhiteSpace(BuildDirectory)) {
                Log.LogWarning($"The {nameof(BuildDirectory)} parameter is required.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(GeneratedSourceDirectory)) {
                Log.LogWarning($"The {nameof(GeneratedSourceDirectory)} parameter is required.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(ProjectSourceDirectory)) {
                Log.LogWarning($"The {nameof(ProjectSourceDirectory)} parameter is required.");
                valid = false;
            }
            return valid;
        }

        private (QmlFileInfo[] Files, bool Valid) CreateQmlFileInfos()
        {
            var valid = true;
            var files = new List<QmlFileInfo>();
            foreach (var item in QmlFiles) {
                var file = CreateQmlFileInfo(item, ref valid);
                if (file != null)
                    files.Add(file);
            }
            return (files.ToArray(), valid);
        }

        private QmlFileInfo? CreateQmlFileInfo(ITaskItem item, ref bool valid)
        {
            var itemValid = true;

            var sourcePath = item.GetMetadata("SourcePath");
            if (string.IsNullOrWhiteSpace(sourcePath))
                sourcePath = item.ItemSpec;
            if (string.IsNullOrWhiteSpace(sourcePath)) {
                Log.LogWarning("A QmlFiles item has no SourcePath or ItemSpec.");
                itemValid = false;
            }

            var modulePath = item.GetMetadata("ModulePath");
            if (string.IsNullOrWhiteSpace(modulePath))
                modulePath = item.GetMetadata("SourceDir");
            if (string.IsNullOrWhiteSpace(modulePath)) {
                Log.LogWarning($"QML file '{sourcePath}' is missing required ModulePath or "
                    + $"SourceDir metadata.");
                itemValid = false;
            }

            var typeName = item.GetMetadata("TypeName");
            if (string.IsNullOrWhiteSpace(typeName)) {
                Log.LogWarning($"QML file '{sourcePath}' is missing required TypeName metadata.");
                itemValid = false;
            }

            valid &= itemValid;
            return itemValid ? new QmlFileInfo(sourcePath, modulePath, typeName) : null;
        }

        private static string[] PrependDistinctPath(string path, IEnumerable<string> paths) =>
            DistinctPaths(new[] { path }.Concat(paths));

        private static string[] AppendDistinctPath(IEnumerable<string> paths, string? path) =>
            DistinctPaths(string.IsNullOrWhiteSpace(path) ? paths : paths.Concat([path!]));

        private static string[] DistinctPaths(IEnumerable<string> paths)
        {
            var result = new List<string>();
            foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path))) {
                if (!result.Any(existing => PathUtilities.AreEquivalent(existing, path)))
                    result.Add(path);
            }
            return result.ToArray();
        }
    }
}
