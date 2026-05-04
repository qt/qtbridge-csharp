// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_QtResource
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task CMake_Includes_Qt_Resources()
        {
            const string source = """
            [assembly: Qt.Bridge.QtResource(
                SourcePath = @"C:\src\icons\close.svg",
                Alias = "assemblies/My.Ui/icons/close.svg",
                AssemblyId = "My.Ui",
                Key = "icons/close.svg")]

            public class Foo { public int Bar { get; set; } }
            """;

            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs: [typeof(Qt.Bridge.QtResourceAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/CMakeLists.txt", out var cmake));
            Assert.Contains("qt_add_resources", cmake);
            Assert.Contains("QT_RESOURCE_ALIAS \"assemblies/My.Ui/icons/close.svg\"", cmake);
            Assert.Contains("\"C:/src/icons/close.svg\"", cmake);
        }

        [TestMethod]
        public async Task DuplicateAlias_SameAssembly_ReportsCollision()
        {
            const string source = """
            [assembly:Qt.Bridge.QtResource(
                SourcePath = @"C:\assets\a\close.svg",
                Alias = "assemblies/Shared/icons/close.svg",
                AssemblyId = "Shared")]
            [assembly:Qt.Bridge.QtResource(
                SourcePath = @"C:\assets\b\close.svg",
                Alias = "assemblies/Shared/icons/close.svg",
                AssemblyId = "Shared")]

            public class Foo { }
            """;

            var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => TestCodeGenerator.GenerateAsync([source],
                    sourceRefs: [typeof(Qt.Bridge.QtResourceAttribute).Assembly],
                    ct: TestContext.CancellationTokenSource.Token));

            Assert.Contains("assemblies/Shared/icons/close.svg", ex.Message);
            Assert.Contains(@"C:\assets\a\close.svg", ex.Message);
            Assert.Contains(@"C:\assets\b\close.svg", ex.Message);
        }

        [TestMethod]
        public async Task DuplicateAlias_CrossAssembly_ReportsCollision()
        {
            const string libSource = """
            [assembly:Qt.Bridge.QtResource(
                SourcePath = @"C:\lib\icons\close.svg",
                Alias = "assemblies/Shared/icons/close.svg",
                AssemblyId = "Shared")]

            public class LibClass { }
            """;

            const string appSource = """
            [assembly:Qt.Bridge.QtResource(
                SourcePath = @"C:\app\icons\close.svg",
                Alias = "assemblies/Shared/icons/close.svg",
                AssemblyId = "Shared")]

            public class AppClass { public LibClass Lib { get; set; } }
            """;

            var apiAssembly = typeof(Qt.Bridge.QtResourceAttribute).Assembly;
            var libPath = CompileToTempAssembly(libSource, [apiAssembly]);

            var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => TestCodeGenerator.GenerateAsync([appSource],
                    sourceRefs: [apiAssembly],
                    referencesWithAliases: [("global", libPath)],
                    ct: TestContext.CancellationTokenSource.Token));

            Assert.Contains("assemblies/Shared/icons/close.svg", ex.Message);
            Assert.Contains(@"C:\lib\icons\close.svg", ex.Message);
            Assert.Contains(@"C:\app\icons\close.svg", ex.Message);
        }

        private static string CompileToTempAssembly(string source, Assembly[] refs)
        {
            var metadataRefs = new List<MetadataReference> {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };
            var runtimeDll = Path.Combine(
                RuntimeEnvironment.GetRuntimeDirectory(), "System.Runtime.dll");
            if (File.Exists(runtimeDll))
                metadataRefs.Add(MetadataReference.CreateFromFile(runtimeDll));
            metadataRefs.AddRange(refs.Select(a => MetadataReference.CreateFromFile(a.Location)));

            var name = "TestLib_" + Guid.NewGuid().ToString("N");
            var compilation = CSharpCompilation.Create(
                name,
                [CSharpSyntaxTree.ParseText(source)],
                metadataRefs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var outputPath = Path.Combine(Path.GetTempPath(), name + ".dll");
            var emitResult = compilation.Emit(outputPath);
            if (!emitResult.Success)
                throw new InvalidOperationException("Library compilation failed: "
                    + string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString())));
            return outputPath;
        }
    }
}
