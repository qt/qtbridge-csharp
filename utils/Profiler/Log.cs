// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using System.Text.RegularExpressions;

namespace Qt.Bridge.Utils.Profiler
{
    public class Log
    {
        public string FilePath { get; internal set; }

        public List<Entry> Entries { get; internal set; }

        public class Entry
        {
            public string Text { get; internal set; }
            public string File { get; internal set; }
            public uint Line { get; internal set; }
            public long Thread { get; internal set; }
            public string Tag { get; internal set; }
            public long Start { get; internal set; }
            public long Stop { get; internal set; }
            public override string ToString() => Text;
        }

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
        private static Regex Parser { get; } = new($@"(?<=^|\r?\n)
            (?<{nameof(Entry.Thread)}>(?:(?!\s\|).)+)\s\|\s
            (?<{nameof(Entry.File)}>(?:(?!\s\|).)+)\s\|\s
            (?<{nameof(Entry.Line)}>(?:(?!\s\|).)+)\s\|\s
            (?<{nameof(Entry.Tag)}>(?:(?!\s\|).)+)\s\|\s
            (?<{nameof(Entry.Start)}>(?:(?!\s\|).)+)\s\|\s
            (?<{nameof(Entry.Stop)}>(?:(?!\r?\n).)+)(?:\r?\n|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

        public static bool TryFind(out string path) => TryFind(null, out path);

        public static bool TryFind(Func<DirectoryInfo, bool> isRoot, out string path)
        {
            path = "";
            isRoot ??= (dir) => dir.Parent == null;

            var dir = new DirectoryInfo(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            while (dir is { Exists: true } && !isRoot(dir))
                dir = dir.Parent;
            if (dir is not { Exists: true })
                return false;
            path = dir.EnumerateFiles("qt_dotnet_profiler.log", SearchOption.AllDirectories)
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .FirstOrDefault();
            return path is { Length: > 0 } && File.Exists(path);
        }

        internal Log() { }

        public static Log Parse(string path)
        {
            if (path is not { Length: > 0 } || !File.Exists(path))
                throw new ArgumentException("Path is incorrect", nameof(path));

            if (path is not { Length: > 0 } || !File.Exists(path))
                throw new FileNotFoundException("Log file was not found", path);

            if (File.ReadAllText(path) is not { Length: > 0 } text)
                throw new InvalidDataException("Log file is empty");

            var log = Parser.Matches(text).Cast<Match>()
                .Select(res => new Entry
                {
                    Text = res.Value,
                    File = res.Groups[nameof(Entry.File)].Value,
                    Line = Convert.ToUInt32(res.Groups[nameof(Entry.Line)].Value),
                    Thread = Convert.ToInt64(res.Groups[nameof(Entry.Thread)].Value, 16),
                    Tag = res.Groups[nameof(Entry.Tag)].Value,
                    Start = Convert.ToInt64(res.Groups[nameof(Entry.Start)].Value),
                    Stop = Convert.ToInt64(res.Groups[nameof(Entry.Stop)].Value)
                })
                .ToList();

            if (log is not { Count: > 0 })
                throw new InvalidDataException("Could not parse log file entries");

            return new()
            {
                FilePath = path,
                Entries = log
            };
        }
    }
}
