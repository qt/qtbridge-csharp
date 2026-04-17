// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary> Fetches QML Language Server release metadata from the Qt release cache. </summary>
    public interface IReleaseMetadataClient
    {
        /// <summary>
        /// Returns the latest QML Language Server release, including the platform-specific asset
        /// download URL and SHA-256 digest.
        /// </summary>
        Task<QmlLanguageServerRelease> GetLatestReleaseAsync(CancellationToken cancellationToken);
    }
}
