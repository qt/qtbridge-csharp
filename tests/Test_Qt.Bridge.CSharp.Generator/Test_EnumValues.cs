// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.Bridge;
    using Support;

    [TestClass]
    public class Test_EnumValues
    {
        public TestContext TestContext { get; set; }

        // Declaration order differs from value order, and from the order of the names.
        private const string Source = """
            using Qt;

            namespace Test
            {
                public enum Shade { Red = 0, Green = -1, Blue = 7, Black = 5 }

                public class Palette
                {
                    public Shade Shade { get; set; }
                }
            }
            """;

        private static readonly (string Name, int Value)[] Declared =
            [("Red", 0), ("Green", -1), ("Blue", 7), ("Black", 5)];

        [TestMethod]
        public async Task SourceCode_EnumValues_MatchDeclaration()
        {
            using var result = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs: [typeof(Qt.ExportAttribute).Assembly],
                defaultExportOptions: Qt.ExportAs.SourceCode,
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/shade.h", out var header),
                string.Join(", ", result.Sink.Files.Keys));
            var values = Regex.Match(header, @"enum Values\s*\{(?<body>[^}]*)\}").Groups["body"]
                .Value.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(entry => entry.Split('='))
                .Select(parts => (parts[0].Trim(), int.Parse(parts[1].Trim())))
                .ToArray();

            CollectionAssert.AreEquivalent(Declared, values);
        }

        [TestMethod]
        [DataRow("source", "GenerateEnum")]
        [DataRow("metadata", "GenerateType")]
        public async Task EnumWithoutValues_IsReportedAsError(string exportAs, string rule)
        {
            const string source = """
                namespace Test
                {
                    public enum Empty { }

                    public class Device
                    {
                        public Empty Mode { get; set; }
                    }
                }
                """;

            await Assert.ThrowsExactlyAsync<System.InvalidOperationException>(
                () => TestCodeGenerator.GenerateAsync([source],
                    sourceRefs:
                    [
                        typeof(Qt.ExportAttribute).Assembly,
                        typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateProperty).Assembly
                    ],
                    defaultExportOptions: exportAs == "source"
                        ? Qt.ExportAs.SourceCode
                        : Qt.ExportAs.Metadata,
                    ct: TestContext.CancellationTokenSource.Token));

            Assert.Contains(collection: Rules.Results, predicate: r => !r.Succeeded
                && r.RuleName == rule && r.Source is System.Type { Name: "Empty" });
        }

        [TestMethod]
        public async Task Metadata_EnumValues_MatchDeclaration()
        {
            using var result = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs:
                [
                    typeof(Qt.ExportAttribute).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateProperty).Assembly
                ],
                defaultExportOptions: Qt.ExportAs.Metadata,
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TestCodeGenerator.MetadataFileName,
                out var json), string.Join(", ", result.Sink.Files.Keys));
            Assert.IsTrue(Metadata.FromJson(json).TypesByName.TryGetValue("Test.Shade",
                out var shade));

            CollectionAssert.AreEquivalent(Declared,
                shade.Qt.Enum.Select(value => (value.Key, (int)value.Value)).ToArray());
        }
    }
}
