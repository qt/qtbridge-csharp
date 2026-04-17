// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Describes a single platform-specific QML Language Server release asset, including its
    /// download URL and SHA-256 digest for integrity verification.
    /// </summary>
    public sealed class QmlLanguageServerAsset(string name, string downloadUrl, string sha256Digest)
    {
        public string Name { get; } = name
            ?? throw new ArgumentNullException(nameof(name));
        public string DownloadUrl { get; } = downloadUrl
            ?? throw new ArgumentNullException(nameof(downloadUrl));
        public string Sha256Digest { get; } = sha256Digest
            ?? throw new ArgumentNullException(nameof(sha256Digest));
    }
}
