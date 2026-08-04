// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

global using Rules = Qt.Bridge.CodeGeneration.Rule.All;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator.Support
{
    using Qt.Bridge.CodeGeneration;
    using Qt.Bridge.CodeGeneration.MetaFunctions;
    using Qt.Bridge.CodeGeneration.Rules.SourceCode.Class;

    /// <summary>
    /// Generates C# code from input sources by compiling them into a temporary
    /// assembly, analyzing dependencies, and applying code generation rules.
    /// </summary>
    public static class TestCodeGenerator
    {
        private static readonly string NewLine = Environment.NewLine;
        private static readonly ConcurrentDictionary<string, byte> TempArtifacts = new();
        private static readonly ConcurrentDictionary<MetadataLoadContext, byte> LoadContexts = new();
        private static string PluginTempRoot => Path.Combine(Path.GetTempPath(), "QtDotNetTests");

        public sealed record Result(DependencyGraph Graph, MetadataLoadContext Loader,
            Assembly SourceAssembly, MemorySink Sink, string TargetDir) : IDisposable
        {
            private int disposed;

            /// <summary>
            /// List of files to include in the combined text. Implicitly asserts that all files in
            /// the list will be present in the sink. This assertion is verified during calculation
            /// of the combined text.
            /// </summary>
            public List<string> SelectedFiles { get; set; } = null;

            /// <summary>Combines all generated files into a single string.</summary>
            public string CombinedText => SelectedFiles switch {
                null => string.Join(NewLine + NewLine, Sink.Files.Values),
                _ => string.Join(NewLine + NewLine, SelectedFiles
                    .Select(file => Sink.Files.TryGetValue(file, out var text) ? text
                        : throw new AssertFailedException($"Selected file not found: {file}"))
                    .Where(text => text != null))
            };

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0)
                    DisposeLoadContext(Loader);
            }
        }

        /// <summary>
        /// Compiles the provided C# sources into a temporary assembly, runs the code generator,
        /// and captures the output in memory.
        /// </summary>
        /// <param name="sources">C# source files to compile.</param>
        /// <param name="sourceRefs">List of reference assemblies</param>
        /// <param name="extraRefs">Additional directories to search for assembly references.</param>
        /// <param name="referencesWithAliases">Aliased references (extern alias support).</param>
        /// <param name="extraRules">Array of custom none build-in rules to apply.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Generated code and metadata.</returns>
        public static async Task<Result> GenerateAsync(string[] sources,
            Assembly[] sourceRefs = null, string[] extraRefs = null,
            List<(string Alias, string Path)> referencesWithAliases = null,
            Type[] extraRules = null,
            CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sources.ToString(), nameof(sources));

            // Ensure no trace left from a previous run
            Placeholder.ResetIndex();
            Rules.Reset();
            FilePlaceholder.All.Reset();

            // Build up necessary dependencies infrastructure
            var refs = CreateDefaultFrameworkPaths()
                .Select(path => MetadataReference.CreateFromFile(path))
                .Cast<MetadataReference>().ToList();

            // Codegen infrastructure + user-specified sourceRefs
            var assemblies = (sourceRefs ?? [])
                .Union(
                [
                    typeof(Rule).Assembly,
                    typeof(GenerateIndexer).Assembly,
                    typeof(BasicTypes).Assembly
                ])
                .Distinct();

            refs.AddRange(assemblies
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location)));

            // Add aliases if provided and update assembly references
            if (referencesWithAliases is { Count: > 0 }) {
                foreach (var (alias, path) in referencesWithAliases) {
                    refs.Add(MetadataReference.CreateFromFile(path,
                        new MetadataReferenceProperties(aliases: ["global", alias])));
                }

                var probe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in extraRefs ?? []) {
                    if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
                        probe.Add(p);
                }
                foreach (var (_, path) in referencesWithAliases) {
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(dir))
                        probe.Add(dir);
                }
                extraRefs = probe.ToArray();
            }

            // Compile input sources into a temporary assembly
            var assemblyName  = "CodeGeneratorTest_" + Guid.NewGuid().ToString("N");
            var trees = sources.Select(src => CSharpSyntaxTree.ParseText(src)).ToArray();
            var compilation = CSharpCompilation.Create(assemblyName , trees, refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var outputPath = Path.Combine(Path.GetTempPath(), assemblyName  + ".dll");
            RegisterTempArtifact(outputPath);
            var emitResult = compilation.Emit(outputPath, cancellationToken: ct);
            if (!emitResult.Success) {
                throw new InvalidOperationException("Emitting the test library failed:" + NewLine
                  + string.Join(NewLine, emitResult.Diagnostics.Select(d => d.ToString())));
            }

            // Set up metadata loading context similar to codegen
            var extraDirectories = new List<string> {
                RuntimeEnvironment.GetRuntimeDirectory(),
                AppContext.BaseDirectory,
                Path.GetDirectoryName(outputPath),
            };
            if (extraRefs is { Length: > 0 })
                extraDirectories.AddRange(extraRefs.Where(Directory.Exists));

            // Ensure Qt.DotNet.Adapter.dll is present among scanned dirs
            var allDlls = extraDirectories
                .SelectMany(d => Directory.EnumerateFiles(d, "*.dll"))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var hasAdapter = allDlls.Any(p => string.Equals(Path.GetFileNameWithoutExtension(p),
                "Qt.DotNet.Adapter", StringComparison.OrdinalIgnoreCase));
            if (!hasAdapter)
                throw new InvalidOperationException("Qt.DotNet.Adapter.dll not found.");

            MetadataLoadContext metadataLoadContext = null;
            try {
                // Create MLC using shared helper
                metadataLoadContext = MetadataResolver.CreateLoadContext(extraDirectories);
                RegisterLoadContext(metadataLoadContext);
                var sourceAssembly = metadataLoadContext.LoadFromAssemblyPath(outputPath);

                // Register meta-functions and rules
                MetaFunction.Register<BasicTypes>();
                foreach (var t in typeof(GenerateIndexer).Assembly.ExportedTypes)
                    _ = t.TryRegisterAsRule();
                foreach (var t in typeof(CppMetaFunction).Assembly.ExportedTypes)
                    _ = t.TryRegisterAsMetaFunction();

                // Register additional none build-in rules
                foreach (var t in extraRules ?? [])
                    _ = t.TryRegisterAsRule();

                // 4. Build dependency graph and run rules
                await DependencyGraph.CreateAsync(metadataLoadContext, sourceAssembly,
                    Array.Empty<Type>());
                var targetDirectory = Path.Combine(Path.GetTempPath(), "qtdotnet_codegen_" + Guid
                    .NewGuid().ToString("N"));
                Directory.CreateDirectory(targetDirectory);
                RegisterTempArtifact(targetDirectory);

                var rulesSucceeded = await Rules.RunAllAsync(targetDirectory);
                if (!rulesSucceeded) {
                    var messages = Rules.Results.Where(result => !result.Succeeded)
                        .Select(result => result.Message);
                    throw new InvalidOperationException(
                        $"Running generation rules failed. Error: {string.Join("\r\n", messages)}");
                }

                // Capture outputs in memory
                var sink = new MemorySink();
                await FilePlaceholder.All.WriteAllAsync(sink, ct);

                return new Result(Rules.SourceGraph, metadataLoadContext, sourceAssembly, sink,
                    targetDirectory);
            } catch {
                DisposeLoadContext(metadataLoadContext);
                throw;
            }
        }

        public static string CreatePluginTempDirectory()
        {
            var dir = Path.Combine(PluginTempRoot, Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);

            RegisterTempArtifact(dir);
            RegisterTempArtifact(PluginTempRoot);

            return dir;
        }

        public static void CleanupTempArtifacts()
        {
            foreach (var loadContext in LoadContexts.Keys) {
                try {
                    loadContext.Dispose();
                } catch {
                    // Ignore cleanup failures at assembly shutdown.
                }
            }
            LoadContexts.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            foreach (var path in TempArtifacts.Keys.OrderByDescending(x => x.Length))
                TryDeleteArtifact(path);
            TempArtifacts.Clear();
        }

        private static void RegisterTempArtifact(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
                TempArtifacts.TryAdd(path, 0);
        }

        private static void RegisterLoadContext(MetadataLoadContext loadContext)
        {
            if (loadContext != null)
                LoadContexts.TryAdd(loadContext, 0);
        }

        private static void DisposeLoadContext(MetadataLoadContext loadContext)
        {
            if (loadContext == null)
                return;

            try {
                LoadContexts.TryRemove(loadContext, out _);
                loadContext.Dispose();
            } catch {
                // Ignore cleanup failures at assembly shutdown.
            }
        }

        private static void TryDeleteArtifact(string path)
        {
            const int attempts = 3;
            for (var i = 0; i < attempts; ++i) {
                try {
                    if (Directory.Exists(path)) {
                        Directory.Delete(path, recursive: true);
                        return;
                    }

                    if (File.Exists(path))
                        File.Delete(path);
                } catch (UnauthorizedAccessException) when (i < attempts - 1) {
                    Thread.Sleep(100);
                } catch (IOException) when (i < attempts - 1) {
                    Thread.Sleep(100);
                } catch (Exception e) {
                    Trace.WriteLine($"Delete failed for '{path}': {e.GetType().Name}: {e.Message}");
                }
            }
        }

        private static IEnumerable<string> CreateDefaultFrameworkPaths()
        {
            var tpa = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            var system = new[]
            {
                "System.Private.CoreLib.dll",
                "System.Runtime.dll",
                "System.Linq.dll",
                "System.Console.dll",
                "System.Collections.dll",
                "System.Runtime.Extensions.dll",
                "netstandard.dll"
            };

            var found = system.Select(name =>
            {
                return tpa.FirstOrDefault(p => string.Equals(Path.GetFileName(p), name,
                    StringComparison.OrdinalIgnoreCase));
            })
            .Where(p => p is not null).ToArray();
            return found.Length == 0 ? [typeof(object).Assembly.Location] : found;
        }
    }
}
