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

        public Data(Log log)
        {
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

            Scopes = scopesByFile.Values
                .SelectMany(x => x)
                .OrderBy(s => s.Tag)
                .ToList();

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
            for (var i = 0; i < Calls.Count; i++) {
                var call = Calls[i];
                if (call.Scope is { } scope)
                    (scope.Calls ??= new(OrderBy.TotalDesc)).Add(call);
                for (var j = i - 1; j >= 0; j--) {
                    var caller = Calls[j];
                    if (caller.Thread != call.Thread)
                        continue;
                    if (caller.Stop < call.Stop)
                        continue;
                    call.Caller = caller;
                    (caller.Calls ??= new(OrderBy.Start)).Add(call);
                    call.Ratio = call.Total / caller.Total;
                    break;
                }
            }

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
            foreach (var callGroup in CallGroups) {
                if (callGroup.Scope is { } scope)
                    (scope.CallGroups ??= new(OrderBy.TotalDesc)).Add(callGroup);
                callGroup.Average = callGroup.Total / callGroup.Calls.Count;
                if (callGroup.Caller is not { } caller)
                    continue;
                (caller.CallGroups ??= new(OrderBy.Start)).Add(callGroup);
                callGroup.Ratio = caller.Total > 0 ? callGroup.Total / caller.Total : 0.0;
            }

            // Functions aggregate call groups with the same context, independently of thread/caller
            // (e.g. a function called several times by different callers and/or different threads)
            Dictionary<(string File, uint Line, Scope Scope, string Tag), Function> funcs = new();
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
            foreach (var func in Functions) {
                if (func.Scope is { } scope)
                    (scope.Functions ??= new(OrderBy.TotalDesc)).Add(func);
                func.Average = func.Total / func.Calls.Count;
                func.Callers = new(func.CallGroups
                    .Select(cg => cg.Caller is not { } caller ? null
                        : funcs[(caller.File, caller.Line, caller.Scope, caller.Tag)])
                    .Where(f => f != null),
                    OrderBy.TotalDesc);
                if (func.Callers is not { Count: > 0 } callers)
                    continue;
                func.Ratio = func.Total / callers.Average(f => f.Total);
                foreach (var caller in callers)
                    (caller.Called ??= new(OrderBy.TotalDesc)).Add(func);
            }
        }
    }
}
