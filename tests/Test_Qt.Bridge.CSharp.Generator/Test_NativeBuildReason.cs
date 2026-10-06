// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.Bridge.CodeGeneration;
using Test_Qt.Bridge.CSharp.Generator.Support;

namespace Test_Qt.Bridge.CSharp.Generator
{
    [TestClass]
    public class Test_NativeBuildReason
    {
        private const string ReasonFile = "native_build_reason.txt";

        private const string Resource = """
            [assembly: Qt.Bridge.QtResource(SourcePath = "C:/assets/a.txt",
                Alias = "assemblies/App/a.txt")]
            """;

        private static readonly System.Reflection.Assembly[] Refs = [
            typeof(Qt.ExportAttribute).Assembly,
            typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateType).Assembly
        ];

        private static string[] Lines(TestCodeGenerator.Result result)
            => result.Sink.Files[ReasonFile].ReplaceLineEndings("\n").Split('\n',
                System.StringSplitOptions.RemoveEmptyEntries);

        [TestMethod]
        public async Task SourceExportListsTheTypeAndNeedsNoAttribute()
        {
            // No attribute and no project fallback: the built-in default is source code.
            using var result = await TestCodeGenerator.GenerateAsync(
                ["public class Example { public int Value { get; set; } }"], sourceRefs: Refs);

            CollectionAssert.AreEqual(
                new[] { "- type 'Example' is exported as source code" }, Lines(result));
        }

        [TestMethod]
        public async Task MixedExportListsOnlyTheSourceTypes()
        {
            using var result = await TestCodeGenerator.GenerateAsync([
                """
                public class DynamicType { }
                [Qt.Export(Options = Qt.ExportAs.SourceCode)]
                public class NativeType { }
                """], sourceRefs: Refs, defaultExportOptions: Qt.ExportAs.Metadata);

            CollectionAssert.AreEqual(
                new[] { "- type 'NativeType' is exported as source code" }, Lines(result));
        }

        [TestMethod]
        public async Task LinkedResourcesAreAReasonWithoutAnyWrapperType()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [Resource + " public class Example { }"], sourceRefs: Refs,
                defaultExportOptions: Qt.ExportAs.Metadata,
                resourcePackaging: ResourcePackaging.Linked);

            CollectionAssert.AreEqual(new[] {
                "- QtResourcePackaging is Linked and 1 native resource(s) must be linked"
            }, Lines(result));
        }

        [TestMethod]
        public async Task TypesAndLinkedResourcesAreBothReported()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [Resource + " public class Example { }"], sourceRefs: Refs,
                resourcePackaging: ResourcePackaging.Linked);

            CollectionAssert.AreEqual(new[] {
                "- type 'Example' is exported as source code",
                "- QtResourcePackaging is Linked and 1 native resource(s) must be linked"
            }, Lines(result));
        }

        [TestMethod]
        [DataRow(ResourcePackaging.Auto)]
        [DataRow(ResourcePackaging.External)]
        [DataRow(ResourcePackaging.Linked)]
        public async Task NoReasonFileWithoutANativeBuild(ResourcePackaging packaging)
        {
            // Linked without resources does not need a native build either.
            using var result = await TestCodeGenerator.GenerateAsync(
                [(packaging == ResourcePackaging.Linked ? "" : Resource) + " public class Example { }"],
                sourceRefs: Refs, defaultExportOptions: Qt.ExportAs.Metadata,
                resourcePackaging: packaging);

            Assert.IsFalse(result.Sink.Files.ContainsKey(ReasonFile));
            Assert.IsFalse(result.Sink.Files.ContainsKey("source_code_status.txt"));
        }

        [TestMethod]
        public async Task ManyTypesAreOrderedAndCapped()
        {
            var types = string.Join(" ", Enumerable.Range(0, 10).Reverse()
                .Select(i => $"public class T{i} {{ }}"));
            using var result = await TestCodeGenerator.GenerateAsync([types], sourceRefs: Refs);

            var lines = Lines(result);
            Assert.HasCount(9, lines);
            for (var i = 0; i < 8; ++i)
                Assert.AreEqual($"- type 'T{i}' is exported as source code", lines[i]);
            Assert.AreEqual("- ...and 2 more type(s)", lines[8]);
        }
    }
}
