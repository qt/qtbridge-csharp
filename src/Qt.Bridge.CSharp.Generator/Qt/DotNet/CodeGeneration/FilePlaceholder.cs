// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

global using Files = Qt.Bridge.CodeGeneration.FilePlaceholder.All;

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Qt.Bridge.Utils.Collections.Concurrent;

namespace Qt.Bridge.CodeGeneration
{

    public class FilePlaceholder : Placeholder
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public bool NewLineEof { get; set; } = true;
        public bool ForceWrite { get; set; } = false;
        public bool ByteOrderMark { get; set; } = false;
        public string IndentChars { get; set; } = "    ";

        public FileInfo Target { get; private set; }

        public FilePlaceholder(string id, MemberInfo src, string path) : base(id, src)
        {
            Indent = 0;
            Sorted = false;
            Target = new FileInfo(Path.Combine(Rules.TargetDir.FullName, path));
            Instances.Add(this);
            AddToIndex();
        }

        public FilePlaceholder(Enum id, MemberInfo src, string path) : this(IdName(id), src, path)
        { }

        public async Task<bool?> WriteAsync(IFileSink sink, CancellationToken cancelToken = default)
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(sink);
#endif
            if (Target == null)
                return null;

            var text = await RenderAsync().WaitAsync(cancelToken).ConfigureAwait(false);
            text = (ByteOrderMark ? "\uFEFF" : "") + text
                .Replace($"{Nul}", "")
                .Replace($"{Tab}", IndentChars);
            text = Regex.Replace(text, $@"[ ]*{BkSpc}", "");
            text = Regex.Replace(text, @"(?<=\n)[ \t]*\r?\n", "")
                .Replace($"{Blank}", "\r\n")
                .TrimEnd('\r', '\n', ' ')
                + (NewLineEof ? "\r\n" : "");

            return await sink.WriteAsync(Target, text, Encoding, ForceWrite, cancelToken)
                .ConfigureAwait(false);
        }

        private static ConcurrentSet<FilePlaceholder> Instances { get; } = new();

        internal static class All
        {
            public static async Task<(FileInfo File, bool? Updated)[]> WriteAllAsync(IFileSink sink,
                CancellationToken cancellationToken = default)
            {
#if DEBUG
                ArgumentNullException.ThrowIfNull(sink);
#endif
                var filePlaceholders = Instances.ToArray(); // snapshot
                var results = new (FileInfo File, bool? Updated)[filePlaceholders.Length];

                await Task.WhenAll(Enumerable.Range(0, filePlaceholders.Length).Select(async i =>
                {
                    var placeholder = filePlaceholders[i];
                    results[i] = (placeholder.Target, await placeholder
                        .WriteAsync(sink, cancellationToken).ConfigureAwait(false));
                })).ConfigureAwait(false);

                return results;
            }

            public static void Reset() => Instances.Clear();
        }

        public static FilePlaceholder operator +(FilePlaceholder self, string text)
        {
            return (FilePlaceholder)((Placeholder)self + text);
        }

        public static FilePlaceholder operator +(FilePlaceholder self, Placeholder placeholder)
        {
            return (FilePlaceholder)((Placeholder)self + placeholder);
        }
    }
}
