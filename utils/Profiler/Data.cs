// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    public class Data
    {
        public Log Log { get; internal set; }
        public List<Scope> Scopes { get; internal set; }
        public List<Call> Calls { get; internal set; }
        public SortedSet<CallGroup> CallGroups { get; internal set; }
        public SortedSet<Function> Functions { get; internal set; }
        public SortedSet<Call> TopCalls { get; internal set; }

        public enum DataContext { Calls, CallGroups, Functions }

        public class ProgressEventArgs(int callIdx, int callCount, DataContext context) : EventArgs
        {
            public int CallIdx => callIdx;
            public int CallCount => callCount;
            public DataContext Context => context;
        }

        public event EventHandler<ProgressEventArgs> Progress;
        private readonly object sync_Progress = new();

        public static class OrderBy
        {
            public static Call.Comparer<Call> Start { get; } = new();
            public static Call.Comparer<Call> TotalDesc { get; } = new()
            {
                OrderBy = c => c.Total,
                Desc = true
            };
            public static Call.Comparer<Call> RatioDesc { get; } = new()
            {
                OrderBy = c => c.Ratio,
                Desc = true
            };
        }

        public async Task AnalyzeAsync(Log log)
        {
            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            // Log entries record context (file, line, thread, tag), start time and stop time.
            Log = log;

            // Special log entries record the scope for subsequent entries
            // (e.g. the name of the type to which called functions belong to)
            var scopesByFile = log.Entries
                .Where(e => e.Stop < 0)
                .Select(e => new Scope
                {
                    File = e.File,
                    Line = e.Line,
                    Tag = e.Tag
                })
                .GroupBy(s => s.File)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Line).ToList());

            Scopes = [.. scopesByFile.Values
                .SelectMany(x => x)
                .OrderBy(s => s.Tag)];

            // Call objects allow linking log entries according to caller/called relation
            // (e.g. a function that calls another function)
            Calls = [.. log.Entries
                .Where(e => e.Stop >= 0)
                .Select(e => new Call
                {
                    Scope = scopesByFile.GetValueOrDefault(e.File)
                        ?.FirstOrDefault(scope => scope.Line < e.Line),
                    File = e.File,
                    Line = e.Line,
                    Tag = e.Tag,
                    Thread = e.Thread,
                    Start = e.Start,
                    Stop = e.Stop,
                    Total = e.Stop - e.Start
                })
                .OrderBy(c => c.Start)];

            var done = 0;
            await Parallel.ForAsync(0, Calls.Count, parallelOptions, async (i, _) =>
            {
                var call = Calls[i];
                if (call.Scope is { } scope) {
                    lock (call.Scope) {
                        (scope.Calls ??= new(OrderBy.TotalDesc)).Add(call);
                    }
                }
                for (var j = i - 1; j >= 0; j--) {
                    var caller = Calls[j];
                    if (caller.Thread != call.Thread)
                        continue;
                    if (caller.Stop < call.Stop)
                        continue;
                    call.Caller = caller;
                    lock (caller) {
                        (caller.Calls ??= new(OrderBy.Start)).Add(call);
                    }
                    call.Ratio = call.Total / caller.Total;
                    break;
                }
                lock (sync_Progress) {
                    done++;
                    Progress?.Invoke(this, new(done, Calls.Count, DataContext.Calls));
                }
            });

            TopCalls = new(Calls, OrderBy.TotalDesc);

            // Call groups aggregate calls with the same context, thread and caller
            // (e.g. a function called several times in a loop)
            CallGroups = new(Calls
                .GroupBy(c => (c.File, c.Line, c.Scope, c.Tag, c.Thread, c.Caller),
                    (g, cs) => new CallGroup
                    {
                        File = g.File,
                        Line = g.Line,
                        Scope = g.Scope,
                        Tag = g.Tag,
                        Thread = g.Thread,
                        Caller = g.Caller,
                        Calls = new(cs, OrderBy.Start),
                        Start = cs.Min(c => c.Start),
                        Stop = cs.Max(c => c.Stop),
                        Total = cs.Sum(c => c.Total)
                    }),
                OrderBy.TotalDesc);

            done = 0;
            await Parallel.ForEachAsync(CallGroups, parallelOptions, async (callGroup, _) =>
            {
                if (callGroup.Scope is { } scope) {
                    lock (scope) {
                        (scope.CallGroups ??= new(OrderBy.TotalDesc)).Add(callGroup);
                    }
                }
                callGroup.Average = callGroup.Total / callGroup.Calls.Count;
                if (callGroup.Caller is { } caller) {
                    lock (caller) {
                        (caller.CallGroups ??= new(OrderBy.Start)).Add(callGroup);
                    }
                    callGroup.Ratio = caller.Total > 0 ? callGroup.Total / caller.Total : 0.0;
                }
                lock (sync_Progress) {
                    done++;
                    Progress?.Invoke(this, new(done, CallGroups.Count, DataContext.CallGroups));
                }
            });

            // Functions aggregate call groups with the same context, independently of thread/caller
            // (e.g. a function called several times by different callers and/or different threads)
            Dictionary<(string File, uint Line, Scope Scope, string Tag), Function> funcs = [];
            Functions = new(CallGroups
                .GroupBy(cg => (cg.File, cg.Line, cg.Scope, cg.Tag),
                    (g, cgs) => funcs[g] = new Function
                    {
                        File = g.File,
                        Line = g.Line,
                        Scope = g.Scope,
                        Tag = g.Tag,
                        Start = cgs.Min(cg => cg.Start),
                        Stop = cgs.Max(cg => cg.Stop),
                        Total = cgs.Sum(cg => cg.Total),
                        Ratio = cgs.Average(cg => cg.Ratio),
                        CallGroups = new(cgs, OrderBy.TotalDesc),
                        Calls = new(cgs.SelectMany(cg => cg.Calls), OrderBy.Start)
                    }),
                OrderBy.TotalDesc);

            done = 0;
            await Parallel.ForEachAsync(Functions, parallelOptions, async (func, _) =>
            {
                if (func.Scope is { } scope) {
                    lock (scope) {
                        (scope.Functions ??= new(OrderBy.TotalDesc)).Add(func);
                    }
                }
                func.Average = func.Total / func.Calls.Count;
                func.Callers = new(func.CallGroups
                    .Select(cg => cg.Caller is not { } caller ? null
                        : funcs[(caller.File, caller.Line, caller.Scope, caller.Tag)])
                    .Where(f => f != null),
                    OrderBy.TotalDesc);
                if (func.Callers is { Count: > 0 } callers) {
                    func.Ratio = func.Total / callers.Average(f => f.Total);
                    foreach (var caller in callers) {
                        lock (caller) {
                            (caller.Called ??= new(OrderBy.TotalDesc)).Add(func);
                        }
                    }
                }
                lock (sync_Progress) {
                    done++;
                    Progress?.Invoke(this, new(done, Functions.Count, DataContext.Functions));
                }
            });
        }
    }
}
