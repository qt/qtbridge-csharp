// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

global using Rules = Qt.Bridge.CodeGeneration.Rule.All;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Qt.Bridge.CodeGeneration
{
    using Utils.Concurrent;
    using Utils.Collections;
    using Utils.Collections.Concurrent;

    public abstract class Rule : IPrioritizable<int>
    {
        public virtual int Priority => 0;

        public virtual IEnumerable<MemberInfo> DependsOn => [];

        public abstract bool Matches(MemberInfo src);

        public virtual async Task<Result> ExecuteAsync(MemberInfo src)
        {
            return await Task.FromResult(Execute(src));
        }

        public virtual Result Execute(MemberInfo src)
        {
            return Error();
        }

        protected const char Nul = Placeholder.Nul;
        protected const char BkSpc = Placeholder.BkSpc;
        protected const char Tab = Placeholder.Tab;
        protected const char Blank = Placeholder.Blank;
        protected const char Wrap = Placeholder.Wrap;
        public const string RootName = DependencyGraph.RootName;
        public static DependencyGraph SourceGraph { get; private set; }
        protected static Type Root => SourceGraph?.Root;

        private static ConcurrentSet<Rule> AllRules { get; } = new();
        public static void Register<T>(T rule) where T : Rule => AllRules.Add(rule);
        public static void Register<T>() where T : Rule, new() => Register(new T());

        public static Type TypeOf(string name) => SourceGraph?.TypeOf(name);
        public static Type TypeOf(Type t) => SourceGraph?.TypeOf(t);
        public static Type TypeOf<T>() => SourceGraph?.TypeOf<T>();

        public struct Result : IPrioritizable<long>
        {
            public long Priority { get; } = Utils.Concurrent.Timestamp.Next();
            public MemberInfo Source { get; set; }
            public Rule Rule { get; set; }
            public string RuleName => Rule?.GetType().Name ?? "<Match>";
            public bool Succeeded { get; set; } = false;
            public string Message { get; set; } = null;
            public string ErrorFile { get; set; }
            public int ErrorLine { get; set; }
            public Result() { }
            public Result(Result that) { this = that; }
            public override string ToString()
            {
                return ($"{(Succeeded ? "OK" : "FAIL")}"
                    + $" {RuleName} [ {Source?.ToString() ?? "???"} ] "
                    + (Message ?? string.Empty))
                    .Trim();
            }
            public string Output => $"{ErrorFile}({ErrorLine}): "
                + $"error: Rule '{RuleName}({Source?.ToString() ?? "???"})'"
                + (string.IsNullOrEmpty(Message) ? "" : ": " + Message);
        }

        protected Result Ok => new Result { Rule = this, Succeeded = true };
        protected Result Error(string msg = null,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return new Result { Rule = this, Message = msg, ErrorFile = file, ErrorLine = line };
        }

        internal static class All
        {
            internal static void Reset()
            {
                SourceGraph = null;
                AllRules.Clear();
                TargetDir = null;
                Results.Clear();
                SourceTypes = null;
                SourceMembers = null;
                SourceRules.Clear();
                SourceStatus = null;
            }

            public static DirectoryInfo TargetDir { get; private set; }

            public static DependencyGraph SourceGraph
            {
                get => Rule.SourceGraph;
                set => Rule.SourceGraph = value;
            }

            public static async Task<bool> RunAllAsync(string targetPath)
            {
                if (SourceGraph == null)
                    return false;
                if (!AllRules.Any())
                    return false;
                if (!Directory.Exists(targetPath))
                    Directory.CreateDirectory(targetPath);
                TargetDir = new DirectoryInfo(targetPath);
                var nodes = SourceGraph.Where(x => !x.Key.IsRootNode());
                SourceTypes = new(nodes.Select(x => x.Key));
                SourceMembers = new(nodes.SelectMany(x => x.Value));
                var sources = SourceTypes.Union(SourceMembers).Prepend(SourceGraph.Root);
                var tests = sources
                    .SelectMany(x => AllRules
                    .Select(y => new { Source = x, Rule = y }));
                _ = Parallel.ForEach(tests, t => Match(t.Source, t.Rule));
                SourceStatus = sources.ToDictionary(x => x, x => new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously));

                var sourceRulesGraph = SourceRules.ToDictionary(
                    sr => sr.Key,
                    sr => sr.Value.SelectMany(r => r.DependsOn.Where(src => src != null)));

                // Check source dependency circularities
                if (sourceRulesGraph.FindCycle() is { } cycle && cycle.Any()) {
                    Results.Add(new()
                    {
                        Source = cycle.First(),
                        Message = "Circular dependency detected"
                    });
                    return false;
                }

                // Check source dependency dead-ends
                var deadEnds = sourceRulesGraph
                    .Where(n => n.Value.Any(d => !sourceRulesGraph.ContainsKey(d)));
                if (deadEnds.Any()) {
                    Results.Add(new()
                    {
                        Source = deadEnds.First().Key,
                        Message = "Unresolved dependency detected"
                    });
                    return false;
                }

                return await RunBatchesAsync();
            }

            public static ConcurrentPriorityList<Result, long> Results { get; } = new();

            private static ConcurrentQueue<Type> SourceTypes { get; set; } = null;
            private static ConcurrentQueue<MemberInfo> SourceMembers { get; set; } = null;

            private static Dictionary<MemberInfo, TaskCompletionSource<bool>> SourceStatus = null;

            private class RuleList : ConcurrentPriorityList<Rule, int> { }

            private static ConcurrentDictionary<MemberInfo, RuleList> SourceRules { get; } = new();

            private static void Match(MemberInfo source, Rule rule)
            {
                if (!rule.Matches(source))
                    return;
                SourceRules
                    .GetOrAdd(source, _ => new RuleList())
                    .Add(rule);
            }

            private static async Task<bool> RunBatchesAsync()
            {
                if (SourceTypes == null)
                    throw new InvalidOperationException("No sources have been set.");
                if (!await (RunBatchAsync([SourceGraph.Root])))
                    return false;
                if (!await RunBatchAsync(SourceTypes))
                    return false;
                if (!await RunBatchAsync(SourceMembers))
                    return false;
                return true;
            }

            private static bool Status(MemberInfo source, bool status)
            {
                SourceStatus[source].TrySetResult(status);
                return status;
            }

            private static async Task<bool> RunBatchAsync(IEnumerable<MemberInfo> batch)
            {
                var results = await Task.WhenAll(batch.Select(source => Task.Run(async () =>
                {
                    if (!SourceRules.TryGetValue(source, out var rules)) {
                        Results.Add(new() { Source = source, Message = "No matching rules" });
                        return Status(source, true);
                    }
                    foreach (var rule in rules) {
                        foreach (var dependency in rule.DependsOn.Where(src => src != null)) {
                            if (!SourceStatus.TryGetValue(dependency, out var dependencyStatus)) {
                                Results.Add(new()
                                {
                                    Source = source,
                                    Message = $"Dependency not in source set: {dependency}"
                                });
                                return Status(source, false);
                            }
                            if (!await dependencyStatus.Task.ConfigureAwait(false)) {
                                Results.Add(new()
                                {
                                    Source = source,
                                    Message = $"Dependency failed: {dependency}"
                                });
                                return Status(source, false);
                            }
                        }
                        var res = new Result(await rule.ExecuteAsync(source)) { Source = source };
                        Results.Add(res);
                        if (res is { Succeeded: false })
                            return Status(source, false);
                    }
                    return Status(source, true);
                })));
                return !results.Contains(false);
            }
        }
    }

    public static class RulesExtensions
    {
        public static bool TryRegisterAsRule(this Type ruleType)
        {
            if (!ruleType.IsAssignableTo(typeof(Rule)))
                return false;
            if (Activator.CreateInstance(ruleType) is not Rule rule)
                return false;
            Rule.Register(rule);
            return true;
        }

        public static bool Is(this Type self, string name) => self == Rule.TypeOf(name);
        public static bool Is(this Type self, Type t) => self == Rule.TypeOf(t);
        public static bool Is<T>(this Type self) => self == Rule.TypeOf<T>();
    }
}
