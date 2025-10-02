/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Test_Qt.DotNet.Generator.Support
{
    using Qt.DotNet.CodeGeneration;
    using Qt.DotNet.CodeGeneration.MetaFunctions;
    using Qt.DotNet.CodeGeneration.Rules.Class;

    /// <summary>
    /// Generates C# code from input sources by compiling them into a temporary
    /// assembly, analyzing dependencies, and applying code generation rules.
    /// </summary>
    public static class TestCodeGenerator
    {
        private static readonly string NewLine = Environment.NewLine;

        public sealed record Result(DependencyGraph Graph, MetadataLoadContext Loader,
            Assembly SourceAssembly, MemorySink Sink, string TargetDir)
        {
            /// <summary>Combines all generated files into a single string.</summary>
            public string CombinedText => string.Join(NewLine + NewLine, Sink.Files.Values);
        }

        /// <summary>
        /// Compiles the provided C# sources into a temporary assembly, runs the code generator,
        /// and captures the output in memory.
        /// </summary>
        /// <param name="sources">C# source files to compile.</param>
        /// <param name="sourceRefs">List of reference assemblies</param>
        /// <param name="extraRefs">Additional directories to search for assembly references.</param>
        /// <param name="referencesWithAliases">Aliased references (extern alias support).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Generated code and metadata.</returns>
        public static async Task<Result> GenerateAsync(string[] sources,
            Assembly[] sourceRefs = null, string[] extraRefs = null,
            List<(string Alias, string Path)> referencesWithAliases = null,
            CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sources.ToString(), nameof(sources));

            // Ensure no trace left from a previous run
            Placeholder.ResetIndex();
            Rule.All.Reset();
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

            // Create MLC using shared helper
            var metadataLoadContext = MetadataResolver.CreateLoadContext(extraDirectories);
            var sourceAssembly = metadataLoadContext.LoadFromAssemblyPath(outputPath);

            // Register meta-functions and rules
            MetaFunction.Register<BasicTypes>();
            foreach (var t in typeof(GenerateIndexer).Assembly.ExportedTypes)
                _ = t.TryRegisterAsRule() || t.TryRegisterAsMetaFunction();

            // Build dependency graph and run rules
            var graph = await DependencyGraph.CreateAsync(metadataLoadContext, sourceAssembly,
                Array.Empty<Type>());
            var targetDirectory = Path.Combine(Path.GetTempPath(), "qtdotnet_codegen_" + Guid
                .NewGuid().ToString("N"));
            Directory.CreateDirectory(targetDirectory);

            var rulesSucceeded = await Rule.All.RunAllAsync(graph, targetDirectory);
            if (!rulesSucceeded)
                throw new InvalidOperationException("Running generation rules failed.");

            // Capture outputs in memory
            var sink = new MemorySink();
            await FilePlaceholder.All.WriteAllAsync(sink, ct);

            return new Result(graph, metadataLoadContext, Assembly.LoadFile(outputPath), sink,
                targetDirectory);
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
