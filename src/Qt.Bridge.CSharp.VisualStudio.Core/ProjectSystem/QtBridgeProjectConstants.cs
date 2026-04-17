// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Well-known identifiers used to detect Qt Bridge projects by static project file analysis.
    /// </summary>
    public static class QtBridgeProjectConstants
    {
        public static readonly IReadOnlyList<string> KnownPackageIdPrefixes =
        [
            "QtGroup.Qt.Bridge.CSharp."
        ];

        public static readonly IReadOnlyList<string> KnownImportedFiles =
        [
            "QtGroup.Qt.Bridge.CSharp.props",
            "QtGroup.Qt.Bridge.CSharp.targets",
            "Qt.Bridge.props",
            "Qt.Bridge.targets"
        ];

        public static readonly IReadOnlyList<string> KnownPropertyNames =
        [
            "QtDotNetPropsImported",
            "QtQmlRootModule",
            "QtQmlSourceDir",
            "QtDir",
            "QtInstallRoot",
            "QtDotNetGen"
        ];

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="packageId"/> matches a known Qt
        /// Bridge NuGet package id prefix.
        /// </summary>
        public static bool IsKnownQtBridgePackageId(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
                return false;

            return KnownPackageIdPrefixes
                .Any(prefix => packageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="importProject"/> contains a known Qt
        /// Bridge import file that ends with <paramref name="suffix"/>.
        /// </summary>
        public static bool IsKnownImportedFile(string importProject, string suffix)
        {
            if (string.IsNullOrWhiteSpace(importProject) || string.IsNullOrWhiteSpace(suffix))
                return false;

            return KnownImportedFiles
                .Where(file => file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .Any(file => importProject.IndexOf(file, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
