// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils
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

            return IsCaseInsensitiveVolume(ToHostSeparators(normalized));
        }

        private static bool IsCaseInsensitiveVolume(string path)
        {
            try {
                DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(path));
                while (directory != null && !directory.Exists)
                    directory = directory.Parent;

                while (directory?.Parent != null) {
                    var alternateName = ChangeCase(directory.Name);
                    if (alternateName != null) {
                        var alternatePath = Path.Combine(directory.Parent.FullName, alternateName);
                        return Directory.Exists(alternatePath);
                    }
                    directory = directory.Parent;
                }
            } catch (Exception exception) when (exception is ArgumentException
                or NotSupportedException or IOException or UnauthorizedAccessException) {
                // Be conservative here, use case-sensitive comparison when the containing
                return false; // volume cannot be inspected.
            }

            return false;
        }

        private static string? ChangeCase(string name)
        {
            for (var index = 0; index < name.Length; ++index) {
                if (!char.IsLetter(name[index]))
                    continue;
                var alternate = char.IsUpper(name[index])
                    ? char.ToLowerInvariant(name[index])
                    : char.ToUpperInvariant(name[index]);
                if (alternate == name[index])
                    continue;
                return name.Substring(0, index) + alternate + name.Substring(index + 1);
            }

            return null;
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
