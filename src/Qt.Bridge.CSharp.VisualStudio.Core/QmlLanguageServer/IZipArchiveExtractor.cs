// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Provides secure extraction of zip archives, ensuring all files are written within the
    /// specified target directory.
    /// </summary>
    public interface IZipArchiveExtractor
    {
        /// <summary>
        /// Extracts all entries from the zip archive at <paramref name="archivePath"/> into
        /// <paramref name="targetDirectory"/>. Throws <see cref="InvalidDataException"/> if
        /// any entry attempts to escape the target directory.
        /// </summary>
        Task ExtractAsync(string archivePath, string targetDirectory, CancellationToken ct);
    }
}
