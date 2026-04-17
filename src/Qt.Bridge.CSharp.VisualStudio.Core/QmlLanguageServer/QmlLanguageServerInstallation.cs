// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Records the state of a successfully installed QML Language Server, including the
    /// resolved executable path, version, and provenance metadata for support diagnostics.
    /// </summary>
    public sealed class QmlLanguageServerInstallation(
        string version,
        string releaseId,
        string installDirectory,
        string executablePath,
        string assetName,
        string downloadUrl,
        string sha256Digest,
        DateTimeOffset installedAtUtc)
    {
        public string Version { get; } = version
            ?? throw new ArgumentNullException(nameof(version));
        public string ReleaseId { get; } = releaseId
            ?? throw new ArgumentNullException(nameof(releaseId));
        public string InstallDirectory { get; } = installDirectory
            ?? throw new ArgumentNullException(nameof(installDirectory));
        public string ExecutablePath { get; } = executablePath
            ?? throw new ArgumentNullException(nameof(executablePath));
        public string AssetName { get; } = assetName
            ?? throw new ArgumentNullException(nameof(assetName));
        public string DownloadUrl { get; } = downloadUrl
            ?? throw new ArgumentNullException(nameof(downloadUrl));
        public string Sha256Digest { get; } = sha256Digest
            ?? throw new ArgumentNullException(nameof(sha256Digest));
        public DateTimeOffset InstalledAtUtc { get; } = installedAtUtc;
    }
}
