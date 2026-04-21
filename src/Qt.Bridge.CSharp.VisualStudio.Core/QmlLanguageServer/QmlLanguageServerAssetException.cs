// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Thrown by <see cref="IReleaseMetadataClient"/> when the release endpoint contains
    /// no asset matching the current platform, or contains ambiguous matches.
    /// Distinct from network or parse failures so callers can map it to
    /// <see cref="QmlLanguageServerInstallError.NoMatchingAsset"/> without referencing
    /// implementation internals.
    /// </summary>
    public sealed class QmlLanguageServerAssetException(string message)
        : Exception(message);
}
