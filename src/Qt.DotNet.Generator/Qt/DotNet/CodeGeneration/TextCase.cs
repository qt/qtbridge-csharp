/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Globalization;
using System.Text.RegularExpressions;

namespace Qt.DotNet.CodeGeneration
{
    public abstract class TextCase
    {
        public abstract IEnumerable<string> Split(string text);
        public abstract string Join(IEnumerable<string> parts);

        protected static string ToLower(string text) => text.ToLowerInvariant();
        protected static string ToUpper(string text) => text.ToUpperInvariant();

        protected static string Capitalize(string s)
        {
            return s.Length switch
            {
                > 1 => char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant(),
                1 => char.ToUpperInvariant(s[0]).ToString(),
                _ => s
            };
        }

        public static CamelCase CamelCase { get; } = new();
        public static PascalCase PascalCase { get; } = new();
        public static SnakeCase SnakeCase { get; } = new();
        public static SnakeCapsCase SnakeCapsCase { get; } = new();
    }

    public class CamelCase : TextCase
    {
        public override IEnumerable<string> Split(string text)
            => Regex.Split(text, @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])");
        public override string Join(IEnumerable<string> parts)
            => string.Join("", parts.Select((x, i) => i == 0 ? ToLower(x) : Capitalize(x)));
    }

    public class PascalCase : TextCase
    {
        public override IEnumerable<string> Split(string text)
            => Regex.Split(text, @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])");
        public override string Join(IEnumerable<string> parts)
            => string.Join("", parts.Select(x => Capitalize(x)));
    }

    public class SnakeCase : TextCase
    {
        public override IEnumerable<string> Split(string text) => text.Split('_');
        public override string Join(IEnumerable<string> parts)
            => string.Join("_", parts.Select(x => ToLower(x)));
    }

    public class SnakeCapsCase : TextCase
    {
        public override IEnumerable<string> Split(string text) => text.Split('_');
        public override string Join(IEnumerable<string> parts)
            => string.Join("_", parts.Select(x => ToUpper(x)));
    }

    public static class StringCaseExtensions
    {
        public static string ToCase<T>(this IEnumerable<string> parts, T textCase)
            where T : TextCase => textCase.Join(parts);

        public static string ToCamelCase(this IEnumerable<string> parts)
            => TextCase.CamelCase.Join(parts);

        public static string ToPascalCase(this IEnumerable<string> parts)
            => TextCase.PascalCase.Join(parts);

        public static string ToSnakeCase(this IEnumerable<string> parts)
            => TextCase.SnakeCase.Join(parts);

        public static string ToSnakeCapsCase(this IEnumerable<string> parts)
            => TextCase.SnakeCapsCase.Join(parts);

        public static IEnumerable<string> FromCase<T>(this string text, T textCase)
            where T : TextCase => textCase.Split(text);

        public static IEnumerable<string> FromCamelCase(this string text)
            => TextCase.CamelCase.Split(text);

        public static IEnumerable<string> FromPascalCase(this string text)
            => TextCase.PascalCase.Split(text);

        public static IEnumerable<string> FromSnakeCase(this string text)
            => TextCase.SnakeCase.Split(text);

        public static IEnumerable<string> FromSnakeCapsCase(this string text)
            => TextCase.SnakeCapsCase.Split(text);
    }
}
