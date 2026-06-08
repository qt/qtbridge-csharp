// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Xml.Linq;

using static Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem.QtBridgeProjectConstants;

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

            var packageReferenceIds = document
                .Descendants()
                .Where(element => element.Name.LocalName == "PackageReference")
                .Select(element => (string?)element.Attribute("Include"))
                .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
                .Select(packageId => packageId!)
                .ToArray();

            var packagePrefixValue = document
                .Descendants()
                .Where(element => element.Parent?.Name.LocalName == "PropertyGroup"
                    && element.Name.LocalName == "QtBridgePackagePrefix")
                .Select(element => element.Value.Trim())
                .FirstOrDefault(value => !string.IsNullOrEmpty(value));

            // Generated app templates reference the bridge package via the templated
            // $(QtBridgePackageId) property, which only resolves to a known package id
            // when combined with a QtBridgePackagePrefix that matches a known prefix.
            var hasTemplatedPackageReference = packagePrefixValue != null
                && IsKnownQtBridgePackagePrefixValue(packagePrefixValue)
                && packageReferenceIds.Any(IsTemplatedQtBridgePackageReference);

            var importedFiles = document
                .Descendants()
                .Where(element => element.Name.LocalName == "Import")
                .Select(element => (string?)element.Attribute("Project"))
                .Where(project => !string.IsNullOrWhiteSpace(project))
                .ToArray();

            var packageReference = packageReferenceIds.FirstOrDefault(IsKnownQtBridgePackageId)
                ?? (hasTemplatedPackageReference ? TemplatedQtBridgePackageId : null);

            var importsQtBridgeProps = importedFiles.Any(path =>
                path != null && IsKnownImportedFile(path, ".props"));

            var importsQtBridgeTargets = importedFiles.Any(path =>
                path != null && IsKnownImportedFile(path, ".targets"));

            var hasKnownQtBridgeProperty = properties.Keys.Any(key =>
                KnownPropertyNames.Contains(key, StringComparer.OrdinalIgnoreCase));

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
                if (!KnownPropertyNames.Contains(
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
