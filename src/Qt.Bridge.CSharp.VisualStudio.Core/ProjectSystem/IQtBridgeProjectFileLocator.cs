// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Locates the enclosing <c>.csproj</c> file by searching parent directories.
    /// </summary>
    public interface IQtBridgeProjectFileLocator
    {
        /// <summary>
        /// Searches parent directories from <paramref name="path"/> and returns the <c>.csproj</c>
        /// found in the first directory that contains exactly one, or <see langword="null"/> if no
        /// unambiguous enclosing project file exists.
        /// </summary>
        string? FindEnclosingProjectFile(string path);
    }
}
