// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    public enum QmlLanguageServerInstallError
    {
        ReleaseMetadataUnavailable,
        NoMatchingAsset,
        DownloadFailed,
        DigestMismatch,
        ExtractionFailed,
        ExecutableNotFound,
        ManifestWriteFailed,
        InstallDirectoryAccessDenied
    }

    /// <summary>
    /// Thrown when the QML Language Server installer fails to acquire or verify the executable.
    /// </summary>
    public sealed class QmlLanguageServerInstallException(
        QmlLanguageServerInstallError error,
        string message,
        Exception? innerException = null)
        : Exception(message, innerException)
    {
        public QmlLanguageServerInstallError Error { get; } = error;
        public string? InstallDirectory { get; set; }
        public string? StagingDirectory { get; set; }
        public string? AssetName { get; set; }
        public string? DownloadUrl { get; set; }
    }
}
