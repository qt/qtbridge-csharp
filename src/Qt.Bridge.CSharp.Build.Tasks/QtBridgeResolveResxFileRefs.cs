// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Xml.Linq;
using System.Runtime.InteropServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Qt.Bridge.Utils;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    public sealed class QtBridgeResolveResxFileRefs : Microsoft.Build.Utilities.Task
    {
        [Required]
        public ITaskItem[] ResxFiles { get; set; } = [];

        [Required]
        public string ProjectDir { get; set; } = string.Empty;

        [Required]
        public string AssemblyResourceId { get; set; } = string.Empty;

        public ITaskItem[]? ResourceAccessOverrides { get; set; }

        [Output]
        public ITaskItem[] ResolvedResources { get; private set; } = [];

        [Output]
        public ITaskItem[] ManagedEmbeddedResxFiles { get; private set; } = [];

        public override bool Execute()
        {
            var resolved = new List<ITaskItem>();
            foreach (var resx in ResxFiles) {
                var resxPath = Path.GetFullPath(resx.ItemSpec);
                if (!File.Exists(resxPath)) {
                    Log.LogError($"Qt Bridge resource file not found: {resx.ItemSpec}");
                    continue;
                }

                var resxDir = Path.GetDirectoryName(resxPath) ?? ProjectDir;
                var document = XDocument.Load(resxPath, LoadOptions.PreserveWhitespace);
                foreach (var data in document.Root?.Elements("data") ?? []) {
                    var name = (string?)data.Attribute("name");
                    var value = data.Element("value")?.Value;
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                        continue;

                    var fileRef = value!.Split(';')[0].Trim();
                    if (string.IsNullOrWhiteSpace(fileRef)
                        || Path.GetInvalidPathChars().Any(fileRef.Contains)) {
                        continue;
                    }

                    var normalizedFileRef = PathUtilities.ToHostSeparators(fileRef);
                    var sourcePath = Path.GetFullPath(Path.Combine(resxDir, normalizedFileRef));
                    if (!File.Exists(sourcePath))
                        continue;

                    var item = new TaskItem(sourcePath);
                    item.SetMetadata("SourcePath", sourcePath);
                    item.SetMetadata("ResxFile", resxPath);
                    item.SetMetadata("Key", $"{Path.GetFileName(resxPath)}::{name}");
                    item.SetMetadata("AssemblyId", AssemblyResourceId);
                    item.SetMetadata("AccessMode", "Default");

                    var relativePath = TryMakeRelativeToProject(sourcePath);
                    item.SetMetadata("Alias", $"assemblies/{AssemblyResourceId}/{relativePath}");
                    resolved.Add(item);
                }
            }

            ApplyResourceAccessOverrides(resolved);
            ResolvedResources = [.. resolved];
            ManagedEmbeddedResxFiles = [.. resolved
                .Where(IsManagedEmbeddingResource)
                .Select(resource => resource.GetMetadata("ResxFile"))
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(GetPathComparer())
                .Select(ITaskItem (path) => new TaskItem(path))];

            return !Log.HasLoggedErrors;
        }

        private void ApplyResourceAccessOverrides(IEnumerable<ITaskItem> resolved)
        {
            if (ResourceAccessOverrides is not { Length: > 0 })
                return;

            var resolvedByKey = resolved.ToDictionary(
                resource => resource.GetMetadata("Key"),
                GetKeyComparer());
            foreach (var access in ResourceAccessOverrides) {
                var key = access.ItemSpec;
                if (!resolvedByKey.TryGetValue(key, out var target)) {
                    Log.LogError($"QtResourceAccess '{key}' does not match any resolved resource. "
                        + "The key must match a resolved 'File.resx::ResourceName' entry.");
                    continue;
                }

                var mode = access.GetMetadata("Mode");
                var reason = access.GetMetadata("Reason");
                if (!string.IsNullOrEmpty(mode))
                    target.SetMetadata("AccessMode", mode);
                if (!string.IsNullOrEmpty(reason))
                    target.SetMetadata("Reason", reason);
            }
        }

        private static bool IsManagedEmbeddingResource(ITaskItem resource)
        {
            var mode = resource.GetMetadata("AccessMode");
            return string.Equals(mode, "ManagedAndNative", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "ManagedOnly", StringComparison.OrdinalIgnoreCase);
        }

        private string TryMakeRelativeToProject(string sourcePath)
        {
            var projectDir = Path.GetFullPath(ProjectDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var pathComparison = GetPathComparison();
            var projectPrefix = projectDir + Path.DirectorySeparatorChar;
            if (!sourcePath.StartsWith(projectPrefix, pathComparison))
                return Path.GetFileName(sourcePath);

            return sourcePath.Substring(projectPrefix.Length).Replace('\\', '/');
        }

        private static StringComparer GetKeyComparer() => StringComparer.Ordinal;

        private static StringComparison GetPathComparison()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }

        private static StringComparer GetPathComparer()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;
        }
    }
}
