// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;

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

        public static bool AreEquivalent(string left, string right)
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var type = IsCaseInsensitive(left) || IsCaseInsensitive(right)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(NormalizeForComparison(left), NormalizeForComparison(right), type);
        }

        public static bool IsCaseInsensitive(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var normalized = ToForwardSlashes(path);
            if (IsWindowsPath(normalized))
                return true;
            if (normalized.StartsWith("/", StringComparison.Ordinal))
                return false;
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        private static string NormalizeForComparison(string path)
        {
            var normalized = ToForwardSlashes(path);
            if (IsWindowsPath(normalized))
                return TrimTrailingSeparators(normalized);
            try {
                var fullPath = ToForwardSlashes(Path.GetFullPath(ToHostSeparators(normalized)));
                return TrimTrailingSeparators(fullPath);
            } catch {
                return TrimTrailingSeparators(normalized);
            }
        }

        private static bool IsWindowsPath(string path)
        {
            return path.Length >= 2
                && char.IsLetter(path[0])
                && path[1] == ':'
                || path.StartsWith("//", StringComparison.Ordinal);
        }

        private static string TrimTrailingSeparators(string path)
        {
            var minimumLength = GetRootLength(path);
            var length = path.Length;
            while (length > minimumLength && path[length - 1] == '/')
                --length;
            return length == path.Length ? path : path.Substring(0, length);
        }

        private static int GetRootLength(string path)
        {
            if (path == "/")
                return 1;
            if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '/')
                return 3;
            return path.Length >= 7 && path.StartsWith("//?/", StringComparison.Ordinal)
                && char.IsLetter(path[4]) && path[5] == ':' && path[6] == '/' ? 7 : 0;
        }
    }
}
