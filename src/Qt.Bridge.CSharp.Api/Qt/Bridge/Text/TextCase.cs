/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.RegularExpressions;

namespace Qt.Bridge.Text
{
    public enum CaseStyle
    {
        Camel,
        Pascal,
        Snake,
        SnakeCaps
    }

    public static class TextCase
    {
        private static readonly Regex CamelPascalSplit = new(
            "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

        public static IEnumerable<string> SplitCase(this string text, CaseStyle style)
        {
            ArgumentNullException.ThrowIfNull(text);

            return style switch {
                CaseStyle.Camel or CaseStyle.Pascal => CamelPascalSplit.Split(text),
                CaseStyle.Snake or CaseStyle.SnakeCaps => text.Split('_'),
                _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
            };
        }

        public static string ToCase(this IEnumerable<string> parts, CaseStyle style)
        {
            ArgumentNullException.ThrowIfNull(parts);

            var list = parts as IList<string> ?? parts.ToList();
            return style switch {
                CaseStyle.Camel => ToCamel(list),
                CaseStyle.Pascal => ToPascal(list),
                CaseStyle.Snake => string.Join("_", list.Select(Lower)),
                CaseStyle.SnakeCaps => string.Join("_", list.Select(Upper)),
                _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
            };

            static string ToCamel(IList<string> ps)
            {
                if (ps.Count == 0)
                    return "";
                var first = Lower(ps[0]);
                return first + string.Concat(ps.Skip(1).Select(CapitalizeLower));
            }

            static string ToPascal(IList<string> ps)
                => string.Concat(ps.Select(CapitalizeLower));
        }

        public static string ConvertCase(this string text, CaseStyle from, CaseStyle to)
            => text.SplitCase(from).ToCase(to);

        private static string Lower(string s) => s?.ToLowerInvariant() ?? "";
        private static string Upper(string s) => s?.ToUpperInvariant() ?? "";

        private static string CapitalizeLower(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            if (s.Length == 1)
                return char.ToUpperInvariant(s[0]).ToString();
            return char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
        }
    }
}
