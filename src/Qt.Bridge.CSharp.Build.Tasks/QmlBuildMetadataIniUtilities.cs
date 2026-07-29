// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.Utils;

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static class QmlBuildMetadataIniUtilities
    {
        public static Dictionary<int, Dictionary<string, string>> ParseWorkspaceEntries(
            IReadOnlyList<string> lines,
            int sectionStart,
            int sectionEnd)
        {
            var entries = new Dictionary<int, Dictionary<string, string>>();
            for (var i = sectionStart + 1; i < sectionEnd; ++i) {
                var line = lines[i].Trim();
                if (!TryParseWorkspaceKey(line, out var index, out var key, out var value))
                    continue;
                if (!entries.TryGetValue(index, out var values)) {
                    values = [];
                    entries[index] = values;
                }
                values[key] = value;
            }

            return entries;
        }

        public static int FindSectionEnd(IReadOnlyList<string> lines, int sectionStart)
        {
            for (var i = sectionStart + 1; i < lines.Count; ++i) {
                var trimmed = lines[i].Trim();
                if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']')
                    return i;
            }
            return lines.Count;
        }

        public static bool ContainsPath(string paths, string path) =>
            paths.Split(Path.PathSeparator).Any(candidate => SameIniPath(candidate, path));

        public static string BuildSectionKey(string path)
        {
            var normalized = PathUtilities.ToForwardSlashes(path).TrimEnd('/');
            if (normalized.Length >= 2 && normalized[1] == ':')
                normalized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
            return "[" + normalized.Replace("/", "<SLASH>") + "]";
        }

        public static bool SameIniPath(string left, string right) =>
            PathUtilities.AreEquivalent(Unquote(left), Unquote(right));

        public static StringComparison SectionComparison(string path)
        {
            return PathUtilities.IsCaseInsensitive(path)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }

        public static string Unquote(string value)
        {
            value = value.Trim();
            return value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"'
                ? value.Substring(1, value.Length - 2)
                : value;
        }

        public static bool TryParseWorkspaceKey(
            string line,
            out int index,
            out string key,
            out string value)
        {
            index = 0;
            key = "";
            value = "";

            var equals = line.IndexOf('=');
            if (equals <= 0)
                return false;

            var separator = line.LastIndexOf('\\', equals - 1, equals);
            if (separator < 0)
                separator = line.LastIndexOf('/', equals - 1, equals);
            if (separator <= 0)
                return false;

            if (!int.TryParse(line.Substring(0, separator), out index))
                return false;

            key = line.Substring(separator + 1, equals - separator - 1);
            value = Unquote(line.Substring(equals + 1));
            return key.Length > 0;
        }
    }
}
