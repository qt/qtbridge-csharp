// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using System.Xml.Linq;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    internal static class ExtensionAssemblyInfo
    {
        public static string GetLocation() =>
            Path.GetDirectoryName(typeof(ExtensionPackage).Assembly.Location) ?? "";
    }

    internal static class ExtensionVersionInfo
    {
        public static string GetInstalledVersion()
        {
            var assemblyDir = ExtensionAssemblyInfo.GetLocation();
            var manifestPath = Path.Combine(assemblyDir, "extension.vsixmanifest");

            if (File.Exists(manifestPath)) {
                try {
                    var document = XDocument.Load(manifestPath);
                    var identity = document.Root?
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "Metadata")?
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "Identity");
                    var version = identity?.Attribute("Version")?.Value;
                    if (!string.IsNullOrWhiteSpace(version))
                        return version!;
                } catch {
                    // Fall back to the assembly version below.
                }
            }

            return typeof(ExtensionPackage).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        }
    }
}
