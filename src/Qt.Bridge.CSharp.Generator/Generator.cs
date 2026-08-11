// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using Qt.Bridge.Utils;

namespace Qt.Bridge.CodeGeneration
{
    using Extensions;
    using static SearchOption;

    internal static class Generator
    {
        private static IEnumerable<string> ResolveRefAssemblies(IEnumerable<string> refs)
        {
            foreach (var reference in refs ?? []) {
                if (string.IsNullOrWhiteSpace(reference))
                    continue;

                if (Directory.Exists(reference)) {
                    foreach (var dll in Directory.GetFiles(reference, "*.dll"))
                        yield return dll;
                    continue;
                }

                if (File.Exists(reference)) {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(reference));
                    if (!string.IsNullOrEmpty(dir)) {
                        foreach (var dll in Directory.GetFiles(dir, "*.dll"))
                            yield return dll;
                    }
                }
            }
        }

        private enum ExitCode
        {
            Ok,
            SourceMissing,
            SourceFileNotFound,
            GraphBuildError,
            GenerationError,
            OutputError
        }

        private static void Info(string msg)
        {
            if (msg is not { Length: > 0 })
                return;
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Qt Bridge: {msg}");
            Console.ForegroundColor = oldForeground;
        }

        private static void Warning(string msg)
        {
            if (msg is not { Length: > 0 })
                return;
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine($"Qt Bridge: Warning: {msg}");
            Console.ForegroundColor = oldForeground;
        }

        private static void Error(string msg)
        {
            if (msg is not { Length: > 0 })
                return;
            var oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Qt Bridge: ERROR: {msg}");
            Console.ForegroundColor = oldForeground;
        }

        private static ExitCode Error(InvocationContext ctx, ExitCode err, string msg = null)
        {
            ctx.ExitCode = (int)err;
            Error(msg);
            return err;
        }

        public enum Options
        {
            Source, Ref, Exclude, Target, Rules, Clean, CleanIgnores
        }

        private static RootCommand Command { get; }
            = new("Qt Bridge for C# - Native Code Generator");

        public static Dictionary<Options, Option> CommandOptions { get; } = new()
        {
            {
                Options.Source, new Option<string>(
                    "--source", "Source assembly file path")
                { Arity = ArgumentArity.ExactlyOne, ArgumentHelpName = "file-path" }
            },
            {
                Options.Target, new Option<string>(
                    "--target", "Path to target dir")
                { Arity = ArgumentArity.ZeroOrOne, ArgumentHelpName = "dir-path" }
            },
            {
                Options.Rules, new Option<string[]>(
                    "--rules", "Load generation rules assembly")
                { Arity = ArgumentArity.ZeroOrMore, ArgumentHelpName = "file-path" }
            },
            {
                Options.Ref, new Option<string[]>(
                    "--ref", "Add file/folder to assembly loader list")
                { Arity = ArgumentArity.ZeroOrMore, ArgumentHelpName = "path" }
            },
            {
                Options.Exclude, new Option<string[]>(
                    "--exclude", "Exclude type from dependency graph")
                { Arity = ArgumentArity.ZeroOrMore, ArgumentHelpName = "type-name" }
            },
            {
                Options.Clean, new Option<bool>(
                    "--clean", "Remove non-generated files from target dir tree")
                { Arity = ArgumentArity.Zero }
            },
            {
                Options.CleanIgnores, new Option<string[]>(
                    "--clean-ignores", "When cleaning, ignore path in target dir tree")
                { Arity = ArgumentArity.OneOrMore, ArgumentHelpName = "path" }
            }
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
                .Union(ResolveRefAssemblies(refs))
                .ToArray();

            var loader = new MetadataLoadContext(new PathAssemblyResolver(assemblies));
            var sourceAssembly = loader.LoadFromAssemblyPath(srcFile.FullName);

            ctx.TryGetValue(Options.Exclude, out string[] excluded);
            var excludedTypes = excluded
                .Select(x => loader.CoreAssembly.GetType(x))
                .Where(x => x != null);

            await DependencyGraph.CreateAsync(loader, sourceAssembly, excludedTypes);
            if (Rules.SourceGraph == null)
                return Error(ctx, ExitCode.GraphBuildError, "Graph build error");

            if (!ctx.TryGetValue(Options.Target, out string targetPath))
                return ExitCode.Ok;

            ctx.TryGetValue(Options.Rules, out string[] ruleFiles);
            foreach (var ruleFile in ruleFiles) {
                Assembly assembly;
                try {
                    assembly = AssemblyLoadContext.Default
                        .LoadFromAssemblyPath(Path.GetFullPath(ruleFile));
                } catch (Exception ex) {
                    return Error(ctx, ExitCode.GenerationError,
                        $"Error loading rules assembly '{ruleFile}': {ex.Message}");
                }
                foreach (var type in assembly.ExportedTypes)
                    _ = type.TryRegisterAsRule() || type.TryRegisterAsMetaFunction();
            }

            var rulesOk = await Rules.RunAllAsync(targetPath);
            foreach (var res in Rules.Results.Where(r => !r.Succeeded))
                Error(res.Output);
            if (!rulesOk)
                return Error(ctx, ExitCode.GenerationError, $@"Error running generation rules");

            foreach (var attrib in Rules.SourceGraph.Root.QtAttributeData()) {
                if (!attrib.AttributeType.Is<Qt.GenerateAttribute>())
                    continue;
                foreach (var genArg in attrib.NamedArguments) {
                    Rules.SourceGraph.Root.GetPlaceholder($"Placeholders.{genArg.MemberName}")
                        ?.AddText(genArg.TypedValue.Value as string);
                }
            }

            var result = await Files.WriteAllAsync(new IncrementalFileSink());
            if (result.Any(x => x.Updated == null || !File.Exists(x.File.FullName)))
                return Error(ctx, ExitCode.OutputError, $@"Error writing generated files");
            result.Where(x => x.Updated == true).ToList()
                .ForEach(x => Console.WriteLine($" {Path.GetRelativePath(targetPath, x.File.FullName)}"));

            var infoMsg = new StringBuilder();
            infoMsg.Append(result.Count(res => res.Updated == true) switch {
                0 => "No new files generated",
                1 => "Generated 1 new file",
                var n => $"Generated {n} new files"
            });
            infoMsg.Append(result.Count(res => res.Updated == false) switch {
                0 => "",
                1 => " (skipped 1 up-to-date file)",
                var n => $" (skipped {n} up-to-date files)"
            });
            Info(infoMsg.ToString());

            if (ctx.TryGetValue(Options.Clean, out bool clean) && clean) {

                if (!ctx.TryGetValue(Options.CleanIgnores, out string[] ignored))
                    ignored = [];

                (var files, var dirs) = Clean(targetPath, ignored, [.. result.Select(r => r.File)]);

                Info(files switch
                {
                    0 => null,
                    1 => "Cleaned up 1 file",
                    var n => $"Cleaned up {n} files"
                });

                Info(dirs switch
                {
                    0 => null,
                    1 => "Cleaned up 1 dir",
                    var n => $"Cleaned up {n} dirs"
                });
            }

            return ExitCode.Ok;
        }

        internal static (int, int) Clean(string targetPath, string[] ignored, FileInfo[] generated)
        {
            (int FilesRemoved, int DirsRemoved) result = default;

            var targetDir = new DirectoryInfo(targetPath);
            var nonGenerated = targetDir.EnumerateFiles("*", AllDirectories)
                .Where(file => (generated ?? []).All(genFile => !genFile.PathEquals(file)));

            var ignoredPaths = (ignored ?? [])
                .Select(path => Path.Combine(targetPath, path))
                .ToList();
            var ignoredFiles = ignoredPaths
                .Where(File.Exists)
                .Select(path => new FileInfo(path));
            var ignoredDirs = ignoredPaths
                .Where(Directory.Exists)
                .Select(path => new DirectoryInfo(path))
                .ToList();

            // Delete non-generated files (except ignored)
            var filesToDelete = nonGenerated
                .Where(file => !ignoredFiles.Any(file.PathEquals)
                    && !ignoredDirs.Any(file.IsSubPathOf))
                .ToList();
            foreach (var file in filesToDelete) {
                try {
                    file.Delete();
                    result.FilesRemoved++;
                }
                catch (Exception) {
                    Warning($"Could not delete file: {file.FullName}");
                }
            }

            // Delete empty dirs (except ignored)
            var checkDirs = new Queue<DirectoryInfo>(targetDir
                .EnumerateDirectories("*", AllDirectories)
                .Where(d => !d.EnumerateFileSystemInfos("*", AllDirectories).Any()));
            while (checkDirs.TryDequeue(out var dir)) {
                if (!dir.IsSubPathOf(targetDir))
                    continue; // fail-safe: ensure no attempt to remove dirs above targetDir
                if (ignoredDirs.Any(xDir => dir.PathEquals(xDir) || dir.IsSubPathOf(xDir)))
                    continue; // Ignored dir
                try {
                    // Assertion: directory exists and is empty
                    if (dir.EnumerateFileSystemInfos("*", AllDirectories).Any())
                        continue; // Directory is not empty
                } catch (DirectoryNotFoundException) {
                    continue; // Directory does not exist
                }
                try {
                    dir.Delete(true);
                    result.DirsRemoved++;
                    if (!dir.Parent.PathEquals(targetDir))
                        checkDirs.Enqueue(dir.Parent);
                } catch (Exception) {
                    Warning($"Could not delete directory: {dir.FullName}");
                }
            }

            return result;
        }
    }

    internal static class CommandLineExtensions
    {
        private static T Default<T>()
        {
            if (!typeof(T).IsArray)
                return default;
            return (T)Convert.ChangeType(
                Array.CreateInstance(typeof(T).GetElementType(), 0), typeof(T));
        }

        public static bool TryGetValue<T>(
            this InvocationContext ctx, Generator.Options opt, out T value)
        {
            value = Default<T>();
            if (Generator.CommandOptions[opt] is not Option<T> option)
                return false;
            if (ctx.ParseResult.HasOption(option) != true)
                return false;
            value = ctx.ParseResult.GetValueForOption(option);
            return true;
        }
    }

}
