// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    internal class Program
    {
        public static async Task Main(string[] args)
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

            var data = new Data();
            data.Progress += Data_Progress;
            try {
                Console.Write($" Parsing log file...\r");
                Console.Out.Flush();
                var log = Log.Parse(path);
                Console.Out.Flush();
                await data.AnalyzeAsync(log);
            } catch (Exception e) {
                Console.Error.WriteLine($@"ERROR: ""{e.Message}""");
                return;
            }

            int top = 40;
            Console.Write($"{new string(' ', ProgressMsg.Length)}\r");
            Console.WriteLine($"# Profiler log");
            Console.WriteLine();
            Console.WriteLine($"`{data.Log.FilePath}`");
            Console.WriteLine();
            Console.WriteLine($"## Top {(data.Functions.Count < top ? "" : $"{top} ")}functions:");
            Console.WriteLine();
            Console.WriteLine(data.Functions.Take(top)
                .Report(Column.Name, Column.Caller, Column.Calls, Column.Average, Column.Total));
        }

        private const string ProgressMsg = " Analyzing@@@@@@@@@@@: ###%            ";

        private static void Data_Progress(object sender, Data.ProgressEventArgs e)
        {
            Console.Write($"{ProgressMsg
                .Replace("@@@@@@@@@@@", e.Context switch
                {
                    Data.DataContext.Calls => " calls",
                    Data.DataContext.CallGroups => " call groups",
                    Data.DataContext.Functions => " functions",
                    _ => string.Empty
                })
                .Replace("###", $"{(e.CallIdx * 100) / e.CallCount}")}\r");
            Console.Out.Flush();
        }
    }
}
