// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Describes a QML Language Server GitHub release, including the platform-specific asset
    /// selected for the current OS and architecture.
    /// </summary>
    public sealed class QmlLanguageServerRelease(
        string releaseId,
        string tagName,
        string htmlUrl,
        string? body,
        DateTimeOffset publishedAt,
        QmlLanguageServerAsset asset)
    {
        public string ReleaseId { get; } = releaseId
            ?? throw new ArgumentNullException(nameof(releaseId));
        public string TagName { get; } = tagName
            ?? throw new ArgumentNullException(nameof(tagName));
        public string HtmlUrl { get; } = htmlUrl
            ?? throw new ArgumentNullException(nameof(htmlUrl));
        public string? Body { get; } = body;
        public DateTimeOffset PublishedAt { get; } = publishedAt;
        public QmlLanguageServerAsset Asset { get; } = asset
            ?? throw new ArgumentNullException(nameof(asset));
    }
}
