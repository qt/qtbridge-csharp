// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        public static IEnumerable<object[]> ResourceBuildCases()
        {
            foreach (var exportMode in new[] { "SourceCode", "Metadata", "Mixed" })
                foreach (var packaging in Enum.GetValues<Qt.Bridge.CodeGeneration.ResourcePackaging>())
                    foreach (var resources in new[] { "Native", "ManagedOnly", "Empty" })
                        yield return [exportMode, packaging, resources];
        }

        [TestMethod]
        [DynamicData(nameof(ResourceBuildCases))]
        public async Task ResourceBuildSpecMatchesExportAndPackaging(
            string exportMode, Qt.Bridge.CodeGeneration.ResourcePackaging packaging, string resources)
        {
            var firstExport = exportMode == "Mixed" ? "SourceCode" : exportMode;
            var secondExport = exportMode == "Mixed" ? "Metadata" : exportMode;
            var resourceAttributes = resources switch {
                "Native" => """
                    [assembly: Qt.Bridge.QtResource(SourcePath = "assets/app icon.png",
                        Alias = "icons/application.png", AccessMode = "Default")]
                    [assembly: Qt.Bridge.QtResource(SourcePath = "assets/app icon.png",
                        Alias = "qt/gui/qguiapplication/windowIcon", AccessMode = "ManagedAndNative")]
                    [assembly: Qt.Bridge.QtResource(SourcePath = "managed.txt",
                        Alias = "managed-only.txt", AccessMode = "ManagedOnly")]
                    """,
                "ManagedOnly" => """
                    [assembly: Qt.Bridge.QtResource(SourcePath = "managed.txt",
                        Alias = "managed-only.txt", AccessMode = "ManagedOnly")]
                    """,
                _ => ""
            };
            using var result = await TestCodeGenerator.GenerateAsync([
                $$"""
                {{resourceAttributes}}
                [Qt.Export(Options = Qt.ExportAs.{{firstExport}})]
                public class First { public int Value { get; set; } }
                [Qt.Export(Options = Qt.ExportAs.{{secondExport}})]
                public class Second { public int Value { get; set; } }
                """],
                sourceRefs: [typeof(Qt.Bridge.QtResourceAttribute).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateType).Assembly],
                defaultExportOptions: Qt.ExportAs.Metadata,
                resourcePackaging: packaging, ct: TestContext.CancellationTokenSource.Token);

            var hasNativeResources = resources == "Native";
            var linked = packaging == Qt.Bridge.CodeGeneration.ResourcePackaging.Linked
                || (packaging == Qt.Bridge.CodeGeneration.ResourcePackaging.Auto && exportMode != "Metadata");
            var files = result.Sink.Files;
            Assert.AreEqual(firstExport == "SourceCode", files.ContainsKey("source/cpp/first.cpp"));
            Assert.AreEqual(secondExport == "SourceCode", files.ContainsKey("source/cpp/second.cpp"));
            var nativeBuild = exportMode != "Metadata" || (linked && hasNativeResources);
            Assert.AreEqual(nativeBuild,
                files.ContainsKey("source_code_status.txt"), "Native-build selection is incorrect.");

            const string linkedQrc = "source/qt_bridge_resources.qrc";
            const string externalQrc = "qt_bridge_resources.qrc";
            Assert.AreEqual(hasNativeResources && linked, files.ContainsKey(linkedQrc));
            Assert.AreEqual(hasNativeResources && !linked, files.ContainsKey(externalQrc));
            Assert.AreEqual(nativeBuild, files.TryGetValue("source/CMakeLists.txt", out var cmake));
            cmake ??= "";
            if (hasNativeResources && linked) {
                Assert.Contains("qt_add_resources(qtbridge_resource_sources qt_bridge_resources.qrc)", cmake);
                Assert.Contains("PRIVATE ${qtbridge_resource_sources})", cmake);
            } else {
                Assert.DoesNotContain("qt_add_resources(", cmake);
                Assert.DoesNotContain("${qtbridge_resource_sources}", cmake);
            }
            if (!hasNativeResources)
                return;

            var entries = XDocument.Parse(files[linked ? linkedQrc : externalQrc])
                .Descendants("file").ToArray();
            CollectionAssert.AreEquivalent(new[] { "icons/application.png",
                    "qt/gui/qguiapplication/windowIcon" },
                entries.Select(file => (string)file.Attribute("alias")).ToArray());
            Assert.IsTrue(entries.All(file => file.Value == "assets/app icon.png"));
        }

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
            var resource = XDocument.Parse(result.Sink.Files["source/qt_bridge_resources.qrc"])
                .Descendants("file").Single();
            Assert.AreEqual("assemblies/My.Ui/icons/close.svg", (string)resource.Attribute("alias"));
            Assert.AreEqual("C:/src/icons/close.svg", resource.Value);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ReferencedLibraryCannotChooseDefaultIcon(bool applicationHasIcon)
        {
            var apiAssembly = typeof(Qt.Bridge.QtResourceAttribute).Assembly;
            var library = CompileToTempAssembly("""
                [assembly: Qt.Bridge.QtResource(SourcePath = "Library.ico",
                    Alias = "qt/gui/qguiapplication/windowIcon")]
                public class LibClass { }
                """, [apiAssembly]);
            try {
                var app = (applicationHasIcon ? """
                    [assembly: Qt.Bridge.QtResource(SourcePath = "App.ico",
                        Alias = "qt/gui/qguiapplication/windowIcon")]
                    """ : "") + " public class AppClass { public LibClass Lib { get; set; } }";
                using var result = await TestCodeGenerator.GenerateAsync([app],
                    sourceRefs: [apiAssembly], referencesWithAliases: [("global", library)],
                    resourcePackaging: Qt.Bridge.CodeGeneration.ResourcePackaging.External,
                    ct: TestContext.CancellationTokenSource.Token);

                if (applicationHasIcon) {
                    var files = XDocument.Parse(result.Sink.Files["qt_bridge_resources.qrc"])
                        .Descendants("file").ToArray();
                    Assert.HasCount(1, files);
                    Assert.AreEqual("App.ico", files[0].Value);
                } else {
                    Assert.IsFalse(result.Sink.Files.ContainsKey("qt_bridge_resources.qrc"));
                }
            } finally {
                File.Delete(library);
            }
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
        [DataRow(Qt.Bridge.CodeGeneration.ResourcePackaging.Linked)]
        [DataRow(Qt.Bridge.CodeGeneration.ResourcePackaging.External)]
        public async Task DuplicateAlias_CrossAssembly_ReportsCollision(
            Qt.Bridge.CodeGeneration.ResourcePackaging packaging)
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
                    defaultExportOptions: Qt.ExportAs.Metadata,
                    resourcePackaging: packaging,
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
