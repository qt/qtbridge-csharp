// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.InteropServices;

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Centralized per-user install path conventions for the QML Language Server under
    /// <c>%LocalAppData%\QtBridge\QmlLanguageServer\</c>.
    /// </summary>
    internal static class QmlLanguageServerPaths
    {
        private const string ToolDirectoryName = "QmlLanguageServer";
        private const string VersionsDirectoryName = "versions";
        private const string CurrentManifestFileName = "current-installation.json";
        private const string InstallationManifestFileName = "installation.json";
        private const string AssetExecutableName = "qmllanguageserver";
        private const string CurrentExecutableName = "qmlls";

        public static string RootDirectory =>
            Path.Combine(QtBridgeUserDataPaths.RootDirectory, ToolDirectoryName);

        public static string VersionsDirectory =>
            Path.Combine(RootDirectory, VersionsDirectoryName);

        public static string CurrentManifestPath =>
            Path.Combine(RootDirectory, CurrentManifestFileName);

        public static string GetInstallDirectory(string version)
        {
            if (!string.IsNullOrWhiteSpace(version))
                return Path.Combine(VersionsDirectory, version);
            throw new ArgumentException("A version is required.", nameof(version));
        }

        public static string GetInstallationManifestPath(string installDir)
        {
            if (!string.IsNullOrWhiteSpace(installDir))
                return Path.Combine(installDir, InstallationManifestFileName);
            throw new ArgumentException("An installation directory is required.", nameof(installDir));
        }

        public static string GetExpectedAssetPrefix()
        {
            var platform = GetPlatform();
            var architecture = GetArchitecture();
            return $"{AssetExecutableName}-{platform}-{architecture}-";
        }

        public static string[] GetCandidateExecutableNames()
        {
            var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
            return
            [
                $"{AssetExecutableName}{extension}",
                $"{CurrentExecutableName}{extension}"
            ];
        }

        private static string GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "windows";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "macos";
            throw new PlatformNotSupportedException("Unsupported platform for QML Language Server.");
        }

        private static string GetArchitecture()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "universal";

            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new PlatformNotSupportedException(
                    "Unsupported architecture for QML Language Server.")
            };
        }
    }
}
