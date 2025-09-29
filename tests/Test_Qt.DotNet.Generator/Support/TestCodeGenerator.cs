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
        /// <param name="extraRefs">Additional directories to search for assembly references.</param>
        /// <param name="maxConcurrency">Maximum parallelism for file operations.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Generated code and metadata.</returns>
        public static async Task<Result> GenerateAsync(string[] sources,
            Assembly[] sourceRefs = null, string[] extraRefs = null,
            int maxConcurrency = 1, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sources.ToString(), nameof(sources));

            // 0. Ensure no trace left from a previous run
            Placeholder.ResetIndex();
            Rule.All.Reset();

            // 1. Compile input sources into a temporary assembly
            var trees = sources.Select(src => CSharpSyntaxTree.ParseText(src)).ToArray();
            var refs = (sourceRefs ?? Array.Empty<Assembly>())
                .Union([
                    Assembly.Load("System.Runtime"),
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(Rule).Assembly,
                    typeof(GenerateIndexer).Assembly,
                    typeof(BasicTypes).Assembly
                ])
                .Distinct()
                .Select(CreateMetadataReference)
                .ToArray();

            var assemblyName  = "CodeGeneratorTest_" + Guid.NewGuid().ToString("N");
            var compilation = CSharpCompilation.Create(assemblyName , trees, refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var outputPath = Path.Combine(Path.GetTempPath(), assemblyName  + ".dll");
            var emitResult = compilation.Emit(outputPath, cancellationToken: ct);
            if (!emitResult.Success) {
                throw new InvalidOperationException("Emitting the test library failed:" + NewLine
                  + string.Join(NewLine, emitResult.Diagnostics.Select(d => d.ToString())));
            }

            // 2. Set up metadata loading context similar to codegen
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
            using var metadataLoadContext = MetadataResolver.CreateLoadContext(extraDirectories);
            var sourceAssembly = metadataLoadContext.LoadFromAssemblyPath(outputPath);

            // 3. Register meta-functions and rules
            MetaFunction.Register<BasicTypes>();
            foreach (var t in typeof(GenerateIndexer).Assembly.ExportedTypes)
                _ = t.TryRegisterAsRule() || t.TryRegisterAsMetaFunction();

            // 4. Build dependency graph and run rules
            var graph = await DependencyGraph.CreateAsync(metadataLoadContext, sourceAssembly,
                Array.Empty<Type>());
            var targetDirectory = Path.Combine(Path.GetTempPath(), "qtdotnet_codegen_" + Guid
                .NewGuid().ToString("N"));
            Directory.CreateDirectory(targetDirectory);

            var rulesSucceeded = await Rule.All.RunAllAsync(graph, targetDirectory);
            if (!rulesSucceeded)
                throw new InvalidOperationException("Running generation rules failed.");

            // 5. Capture outputs in memory
            var sink = new MemorySink();
            await FilePlaceholder.All.WriteAllAsync(sink, ct);

            return new Result(graph, metadataLoadContext, Assembly.LoadFile(outputPath), sink,
                targetDirectory);
        }

        private static PortableExecutableReference CreateMetadataReference(Assembly a)
            => MetadataReference.CreateFromFile(a.Location);
    }
}
