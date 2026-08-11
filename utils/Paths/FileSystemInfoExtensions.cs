// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils
{
    internal static class FileSystemInfoExtensions
    {
        public static bool PathEquals(this FileSystemInfo? self, FileSystemInfo? other)
        {
            return (self, other) switch
            {
                (null, _) or (_, null) => false,
                _ => PathUtilities.AreEquivalent(self.FullName, other.FullName)
            };
        }

        public static bool IsSubPathOf(this FileSystemInfo? self, DirectoryInfo? other)
        {
            var dir = self switch
            {
                FileInfo f => f.Directory,
                DirectoryInfo d when !d.PathEquals(other) => d,
                _ => null
            };
            while (dir != null) {
                if (dir.PathEquals(other))
                    return true;
                dir = dir.Parent;
            }
            return false;
        }
    }
}
