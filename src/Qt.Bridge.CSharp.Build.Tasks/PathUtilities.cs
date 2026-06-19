// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static class PathUtilities
    {
        public static string ToForwardSlashes(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            return path.Replace('\\', '/');
        }

        public static string ToHostSeparators(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            return path.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
