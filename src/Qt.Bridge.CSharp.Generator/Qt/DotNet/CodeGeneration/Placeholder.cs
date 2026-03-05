// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Qt.Bridge.CodeGeneration
{
    using Utils.Concurrent;

    public class Placeholder
    {
        public const int AutoIndent = -1;
        public int Indent { get; set; } = AutoIndent;
        public bool Sorted { get; set; } = true;
        public bool Distinct { get; set; } = false;

        public string Id { get; private set; }
        public MemberInfo Source { get; set; }

        public IEnumerable<string> Content {
            init => value.ToList().ForEach(text => AddText(text));
        }

        public Placeholder(string id = null, MemberInfo src = null)
        {
            Id = id ?? Path.GetRandomFileName();
            Source = src;
        }

        public Placeholder(Enum id, MemberInfo src = null) : this(IdName(id), src)
        { }

        public virtual Placeholder AddText(string text) => AddText(AutoIndent, text);

        public Placeholder AddText(int indent, string text)
        {
            var refs = Regex.Matches(text, @"(?<=\n)(?<Indent>[ ]*)(?<Ref>[\uE000-\uEFFF])");
            var children = Children.ToArray();
            foreach (Match match in refs) {
                if (match.Groups["Ref"] is not { Success: true } refMatch)
                    continue;
                var refIdx = refMatch.Value[0] - '\uE000';
                if (children.ElementAtOrDefault(refIdx) is not { Indent: AutoIndent } child)
                    continue;
                var indentValue = string.Empty;
                if (match.Groups["Indent"] is { Success: true } indentMatch)
                    indentValue = indentMatch.Value;
                child.Indent = indent switch
                {
                    AutoIndent => indentValue.Length / 4,
                    _ => indent
                };
            }

            text = Regex.Replace(text, $@"{Wrap}\s*", "");
            text = text.Trim('\r', '\n', ' ').Replace($"\r\n{Blank}", $"{Blank}") + End;
            text = Regex.Replace(text, @"(?<=\n)[ ]+(?=[\uE000-\uEFFF])", "");
            text = Regex.Replace(text, @"(?<=\n)\r?\n", $"{Blank}");

            lock (criticalSection)
                Text.Append(text);

            return this;
        }

        public virtual Placeholder AddPlaceholder(Placeholder placeholder)
        {
            if (Connect(this, placeholder))
                AddText($"{RefIdx(Array.IndexOf(Children.ToArray(), placeholder))}");
            return placeholder;
        }

        public virtual char RefPlaceholder(Placeholder placeholder)
        {
            if (placeholder.Parent == this || Connect(this, placeholder))
                return RefIdx(Array.IndexOf(Children.ToArray(), placeholder));
            return Nul;
        }

        public static Placeholder operator +(Placeholder self, string text)
        {
            self.AddText(text);
            return self;
        }

        public static Placeholder operator +(Placeholder self, Placeholder placeholder)
        {
            self.AddPlaceholder(placeholder);
            return self;
        }

        public char this[Placeholder placeholder] => RefPlaceholder(placeholder);

        public void Reset()
        {
            lock (criticalSection) {
                foreach (var child in Children) {
                    child.Reset();
                    child.RemoveFromIndex();
                    child.Parent = null;
                }
                Children.Clear();
                Text.Clear();
            }
        }

        public const char Nul = '\uF000';
        public const char BkSpc = '\uF008';
        public const char Tab = '\uF009';
        public const char Blank = '\uF00A';
        public const char Wrap = '\uF00D';
        protected const char End = '\uF017';

        protected const char RefBase = '\uE000';
        protected const char RefError = '\uF0EE';

        private static char RefIdx(int index)
        {
            if (index is < 0 or > 0xFFF)
                return RefError;
            return (char)(RefBase + index);
        }

        private readonly object criticalSection = new();

        private StringBuilder Text { get; } = new();
        private Placeholder Parent { get; set; } = null;
        private ConcurrentQueue<Placeholder> Children { get; } = new();
        private static ConcurrentDictionary
            <(MemberInfo, string), Placeholder> Index { get; } = new();

        internal static void ResetIndex()
        {
            Index.Clear();
        }

        protected static string IdName(Enum id) => $"{id.GetType().Name}.{id}";

        protected void AddToIndex()
        {
            if (!Index.TryAdd((Source, Id), this)) {
                throw new InvalidOperationException(
                    $"Duplicate placeholder definition for (${Source}, ${Id})");
            }
        }

        protected void RemoveFromIndex()
        {
            if (!Index.TryGetValue((Source, Id), out var placeholder))
                return;
            // If the pair '(this.Source, this.Id)' is in the index, it can only map to 'this'.
            Debug.Assert(placeholder == this);
            if (placeholder != this)
                return;
            Index.TryRemove((Source, Id), out _);
        }

        private static bool Connect(Placeholder parent, Placeholder child)
        {
            if (child is FilePlaceholder)
                return false;
            lock (child.criticalSection) {
                if (child.Parent != null)
                    return false;
                lock (parent.criticalSection) {
                    child.Parent = parent;
                    child.Source ??= parent.Source;
                    parent.Children.Enqueue(child);
                    child.AddToIndex();
                }
            }
            return true;
        }

        private class TextComparer : IComparer<string>, IEqualityComparer<string>
        {
            private string N(string s)
            {
                return Regex
                    .Replace(s, $@"[{Nul},{Tab},{End},{BkSpc},{Blank},{RefBase}-{RefError}]+", "");
            }

            public int Compare(string x, string y)
            {
                return StringComparer.Ordinal.Compare(N(x), N(y));
            }

            public bool Equals(string x, string y)
            {
                return StringComparer.Ordinal.Equals(N(x), N(y));
            }

            public int GetHashCode([DisallowNull] string obj)
            {
                return StringComparer.Ordinal.GetHashCode(N(obj));
            }
        }

        private static TextComparer Comparer { get; } = new();

        protected virtual async Task<string> RenderAsync()
        {
            if (Indent == AutoIndent)
                Indent = 0;

            var text = Text.ToString();
            var childrenText = await Task.WhenAll(Children.ToArray()
                .Select(x => Task.Run(async () => await x.RenderAsync())));
            for (int i = 0; i < childrenText.Length; ++i)
                text = text.Replace($"{RefIdx(i)}", childrenText[i]);

            IEnumerable<string> blocks = text
                .Split(End, StringSplitOptions.RemoveEmptyEntries);
            if ((Sorted || Distinct) && blocks.Count() > 1) {
                blocks = blocks.Order(Comparer);
                if (Distinct)
                    blocks = blocks.Distinct(Comparer);
            }
            text = string.Join("\r\n", blocks);
            var lines = text
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            text = string.Join("\r\n", lines
                .Select(x => $"{new string(Tab, Indent)}{x}"));
            return text;
        }

        public static Placeholder Get(Enum id, MemberInfo src)
        {
            return Get(IdName(id), src);
        }

        public static Placeholder Get(string id, MemberInfo src)
        {
            if (!Index.TryGetValue((src, id), out var placeholder))
                return null;
            return placeholder;
        }

        private class Alias : Placeholder
        {
            private Placeholder Actual { get; set; }

            public Alias(Placeholder actual, string id, MemberInfo src) : base(id, src)
            {
                Actual = actual;
                AddToIndex();
            }

            public Alias(Placeholder actual, Enum id, MemberInfo src) : base(id, src)
            {
                Actual = actual;
                AddToIndex();
            }

            public override Placeholder AddText(string text)
            {
                return Actual.AddText(text);
            }

            public override Placeholder AddPlaceholder(Placeholder placeholder)
            {
                placeholder.Source ??= Source;
                return Actual.AddPlaceholder(placeholder);
            }

            public override char RefPlaceholder(Placeholder placeholder)
            {
                placeholder.Source ??= Source;
                return Actual.RefPlaceholder(placeholder);
            }

            protected override async Task<string> RenderAsync()
            {
                Debug.Assert(false, "Alias placeholders must not be rendered.");
                return await Task.FromResult(string.Empty);
            }
        }

        public Placeholder CreateAlias(MemberInfo src, string id = null)
        {
            return new Alias(this, id ?? Id, src);
        }

        public Placeholder CreateAlias(MemberInfo src, Enum id) {
            return new Alias(this, id, src);
        }
    }

    public static class SourcePlaceholderExtensions
    {
        public static Placeholder GetPlaceholder(this MemberInfo src, string id)
            => Placeholder.Get(id, src);

        public static Placeholder GetPlaceholder(this MemberInfo src, Enum id)
            => Placeholder.Get(id, src);
    }
}
