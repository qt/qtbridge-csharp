/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

global using Rules = Qt.DotNet.CodeGeneration.Rule.All;

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Qt.DotNet.CodeGeneration
{
    using Utils.Concurrent;
    using Utils.Collections.Concurrent;

    public abstract class Rule : IPrioritizable<int>
    {
        public virtual int Priority => 0;

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
                var tests = SourceTypes.Union(SourceMembers).Prepend(SourceGraph.Root)
                    .SelectMany(x => AllRules
                    .Select(y => new { Source = x, Rule = y }));
                _ = Parallel.ForEach(tests, t => Match(t.Source, t.Rule));
                return await RunBatchesAsync();
            }

            public static ConcurrentPriorityList<Result, long> Results { get; } = new();

            private static ConcurrentQueue<Type> SourceTypes { get; set; } = null;
            private static ConcurrentQueue<MemberInfo> SourceMembers { get; set; } = null;


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

            private static async Task<bool> RunBatchAsync(IEnumerable<MemberInfo> batch)
            {
                var results = await Task.WhenAll(batch.Select(source => Task.Run(async () =>
                {
                    if (!SourceRules.TryGetValue(source, out var rules)) {
                        Results.Add(new() { Source = source, Message = "No matching rules" });
                        return false;
                    }
                    foreach (var rule in rules) {
                        var res = new Result(await rule.ExecuteAsync(source)) { Source = source };
                        Results.Add(res);
                        if (res is { Succeeded: false })
                            return false;
                    }
                    return true;
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
