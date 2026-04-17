// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Xml.Linq;

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Detects whether a <c>.csproj</c> file represents a Qt Bridge project by static XML
    /// analysis of package references, imported files, and well-known MSBuild properties.
    /// </summary>
    public sealed class QtBridgeProjectDetector : IQtBridgeProjectDetector
    {
        public Task<QtBridgeProjectMetadata> DetectAsync(
            string projectFilePath,
            CancellationToken cancellationToken = default)
        {
            return string.IsNullOrWhiteSpace(projectFilePath)
                ? throw new ArgumentException("Project file path missing.", nameof(projectFilePath))
                : Task.Run(() => DetectCore(projectFilePath, cancellationToken), cancellationToken);
        }

        private static QtBridgeProjectMetadata DetectCore(
            string projectFilePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.GetFullPath(projectFilePath);
            var document = XDocument.Load(fullPath,
                LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var properties = CollectProperties(document);

            var packageReference = document
                .Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Select(element => (string?)element.Attribute("Include"))
                .FirstOrDefault(packageId => packageId != null
                    && QtBridgeProjectConstants.IsKnownQtBridgePackageId(packageId));

            var importedFiles = document
                .Descendants()
                .Where(element => element.Name.LocalName == "Import")
                .Select(element => (string?)element.Attribute("Project"))
                .Where(project => !string.IsNullOrWhiteSpace(project))
                .ToArray();

            var importsQtBridgeProps = importedFiles.Any(path =>
                path != null && QtBridgeProjectConstants.IsKnownImportedFile(path, ".props"));

            var importsQtBridgeTargets = importedFiles.Any(path =>
                path != null && QtBridgeProjectConstants.IsKnownImportedFile(path, ".targets"));

            var hasKnownQtBridgeProperty = properties.Keys.Any(key =>
                QtBridgeProjectConstants.KnownPropertyNames
                    .Contains(key, StringComparer.OrdinalIgnoreCase));

            var isQtBridgeProject = packageReference != null
                || importsQtBridgeProps
                || importsQtBridgeTargets
                || hasKnownQtBridgeProperty;

            return new QtBridgeProjectMetadata(
                projectFilePath: fullPath,
                type: isQtBridgeProject
                    ? QtBridgeProjectType.QtBridgeCSharp
                    : QtBridgeProjectType.Unknown,
                isQtBridgeProject: isQtBridgeProject,
                matchedPackageId: packageReference,
                importsQtBridgeProps: importsQtBridgeProps,
                importsQtBridgeTargets: importsQtBridgeTargets,
                properties: properties);
        }

        private static IReadOnlyDictionary<string, string> CollectProperties(XContainer document)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var propertyElement in document
                .Descendants()
                .Where(element => element.Parent?.Name.LocalName == "PropertyGroup")) {
                if (!QtBridgeProjectConstants.KnownPropertyNames.Contains(
                    propertyElement.Name.LocalName,
                    StringComparer.OrdinalIgnoreCase)) {
                    continue;
                }

                var value = propertyElement.Value.Trim();
                if (string.IsNullOrEmpty(value))
                    continue;

                properties[propertyElement.Name.LocalName] = value;
            }

            return properties;
        }
    }
}
