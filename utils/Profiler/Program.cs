// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var path = args.FirstOrDefault();
            if (path is not { Length: > 0 } || !File.Exists(path)) {
                Func<DirectoryInfo, bool> solutionDir = dir =>
                    dir.EnumerateFiles("qtbridge-csharp.sln", SearchOption.TopDirectoryOnly).Any();
                if (!Log.TryFind(solutionDir, out path)) {
                    Console.Error.WriteLine("ERROR: profiler log file not found");
                    return;
                }
            }

            Data data;
            try {
                data = new Data(Log.Parse(path));
            } catch (Exception e) {
                Console.Error.WriteLine($@"ERROR: ""{e.Message}""");
                return;
            }

            Console.WriteLine($"# Profiler log");
            Console.WriteLine();
            Console.WriteLine($"`{data.Log.FilePath}`");
            Console.WriteLine();
            Console.WriteLine($"## Top {(data.TopCalls.Count < 20 ? "" : "20 ")}calls:");
            Console.WriteLine();
            Console.WriteLine(data.TopCalls.Take(20)
                .Report(Column.Name, Column.Caller, Column.Total));
            Console.WriteLine($"## Top {(data.Functions.Count < 20 ? "" : "20 ")}functions:");
            Console.WriteLine();
            Console.WriteLine(data.Functions.Take(20)
                .Report(Column.Name, Column.Caller, Column.Calls, Column.Average, Column.Total));
        }
    }
}
