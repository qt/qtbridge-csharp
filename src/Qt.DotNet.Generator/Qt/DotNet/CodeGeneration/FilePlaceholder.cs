/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

global using Files = Qt.DotNet.CodeGeneration.FilePlaceholder.All;

using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Qt.DotNet.CodeGeneration
{
    using Utils.Collections.Concurrent;

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

        public async Task<bool?> WriteAsync()
        {
            if (Target == null)
                return null;

            var text = await RenderAsync();
            text = (ByteOrderMark ? "\uFEFF" : "") + text
                .Replace($"{Nul}", "")
                .Replace($"{Tab}", IndentChars);
            text = Regex.Replace(text, $@"[ ]*{BkSpc}", "");
            text = Regex.Replace(text, @"(?<=\n)[ \t]*\r?\n", "")
                .Replace($"{Blank}", "\r\n")
                .TrimEnd('\r', '\n', ' ')
                + (NewLineEof ? "\r\n" : "");
            var data = Encoding.GetBytes(text);

            if (!ForceWrite && Target.Exists) {
                using var newData = new MemoryStream(data);
                var newSha1 = await SHA1.HashDataAsync(newData);
                try {
                    await using var oldData = Target.OpenRead();
                    var oldSha1 = await SHA1.HashDataAsync(oldData);
                    if (Enumerable.SequenceEqual(newSha1, oldSha1))
                        return false;
                } catch (SystemException) {
                    return null;
                }
            }

            try {
                if (Target.Directory is { Exists: false, FullName.Length: > 0 })
                    Directory.CreateDirectory(Target.Directory.FullName);
                await using var file = Target
                    .Open(FileMode.Create, FileAccess.Write, FileShare.None);
                await file.WriteAsync(data);
                await file.FlushAsync();
                Target.Refresh();
            } catch (SystemException) {
                return null;
            }

            return true;
        }

        private static ConcurrentSet<FilePlaceholder> Instances { get; } = new();

        internal static new class All
        {
            public static async Task<(FileInfo File, bool? Updated)[]> WriteAllAsync()
            {
                return await Task.WhenAll(Instances
                    .Select(x => Task.Run(async () => (x.Target, await x.WriteAsync()))));
            }
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
