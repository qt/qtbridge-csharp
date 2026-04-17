// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary>
    /// Locates the enclosing <c>.csproj</c> file by searching parent directories.
    /// </summary>
    public sealed class QtBridgeProjectFileLocator : IQtBridgeProjectFileLocator
    {
        public string? FindEnclosingProjectFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var currentDir = Directory.Exists(path)
                ? new DirectoryInfo(path)
                : new FileInfo(path).Directory;

            while (currentDir != null) {
                var projectFiles = currentDir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
                if (projectFiles.Length == 1)
                    return projectFiles[0].FullName;

                // Do not guess when a directory contains multiple sibling projects. Searching
                // upward keeps this fallback locator conservative until a better project system
                // aware ownership check is available.
                currentDir = currentDir.Parent;
            }

            return null;
        }
    }
}
