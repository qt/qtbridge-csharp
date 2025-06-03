/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Qt.DotNet.CodeGeneration
{
    internal class Generator
    {
        private enum ExitCode
        {
            Ok,
            SourceMissing,
            SourceFileNotFound,
            GraphBuildError,
            GenerationError,
            OutputError,
#if DEBUG
            GraphVizError = 99
#endif
        }

        private static ExitCode Error(InvocationContext ctx, ExitCode err, string msg = null)
        {
            ctx.ExitCode = (int)err;
            if (msg == null)
                return err;
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.ForegroundColor = oldForeground;
            return err;
        }

        public enum Options
        {
#if DEBUG
            Graphviz,
#endif
            Source, Ref, Exclude, Target, Rules
        }

        private static RootCommand Command { get; } = new("Qt/.NET Native Code Generator");

        public static Dictionary<Options, Option> CommandOptions { get; } = new()
        {
            {
                Options.Source, new Option<string>(
                    "--source", "Source assembly file path")
                { Arity = ArgumentArity.ExactlyOne }
            },
            {
                Options.Target, new Option<string>(
                    "--target", "Path to target dir")
                { Arity = ArgumentArity.ZeroOrOne }
            },
            {
                Options.Rules, new Option<string[]>(
                    "--rules", "Load generation rules assembly")
                { Arity = ArgumentArity.ZeroOrMore }
            },
            {
                Options.Ref, new Option<string[]>(
                    "--ref", "Add file/folder to assembly loader list")
                { Arity = ArgumentArity.ZeroOrMore }
            },
            {
                Options.Exclude, new Option<string[]>(
                    "--exclude", "Exclude type from dependency graph")
                { Arity = ArgumentArity.ZeroOrMore }
            },
#if DEBUG
            {
                Options.Graphviz, new Option<string>(
                    "--graphviz", "Debug visualization")
                { Arity = ArgumentArity.ZeroOrOne }
            }
#endif
        };

        private static async Task<int> Main(string[] args)
        {
            foreach (var option in CommandOptions.Values)
                Command.AddOption(option);
            Command.SetHandler(ExecuteAsync);
            return await Command.InvokeAsync(args);
        }

        private static async Task<ExitCode> ExecuteAsync(InvocationContext ctx)
        {
            if (!ctx.TryGetValue(Options.Source, out string src))
                return Error(ctx, ExitCode.SourceMissing, $@"Missing --source option");

            if (new FileInfo(src) is not { Exists: true } srcFile)
                return Error(ctx, ExitCode.SourceFileNotFound, $@"File not found: '{src}'");

            ctx.TryGetValue(Options.Ref, out string[] refs);
            var assemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
                .Union(Directory.GetFiles(Environment.CurrentDirectory, "*.dll"))
                .Union(Directory.GetFiles(srcFile.DirectoryName, "*.dll"))
                .Union(refs.SelectMany(x => Directory.GetFiles(
                    Directory.Exists(x) ? x : Path.GetDirectoryName(x), "*.dll")))
                .ToArray();

            var loader = new MetadataLoadContext(new PathAssemblyResolver(assemblies));
            var sourceAssembly = loader.LoadFromAssemblyPath(srcFile.FullName);

            ctx.TryGetValue(Options.Exclude, out string[] excluded);
            var excludedTypes = excluded
                .Select(x => loader.CoreAssembly.GetType(x))
                .Where(x => x != null);

            var graph = await DependencyGraph.CreateAsync(loader, sourceAssembly, excludedTypes);
            if (graph == null)
                return Error(ctx, ExitCode.GraphBuildError, "Graph build error");
#if DEBUG
            if (ctx.TryGetValue(Options.Graphviz, out string graphVizPath)) {
                try {
                    GraphViz
                        .FromDependencyGraph(graph)
                        .ToPdfFile(graphVizPath);
                } catch (Exception ex) {
                    return Error(ctx, ExitCode.GraphVizError,
                        $"Error generating GraphViz content: '{ex.Message}'");
                }
            }
#endif
            return ExitCode.Ok;
        }
    }

    internal static class CommandLineExtensions
    {
        private static T Default<T>()
        {
            if (!typeof(T).IsArray)
                return default;
            Array.Empty<object>();
            return (T)Convert.ChangeType(
                Array.CreateInstance(typeof(T).GetElementType(), 0), typeof(T));
        }

        public static bool TryGetValue<T>(
            this InvocationContext ctx, Generator.Options opt, out T value)
        {
            value = Default<T>();
            if (Generator.CommandOptions[opt] is not Option<T> option)
                return false;
            if (ctx.ParseResult?.HasOption(option) != true)
                return false;
            value = ctx.ParseResult.GetValueForOption(option);
            return true;
        }
    }
}
