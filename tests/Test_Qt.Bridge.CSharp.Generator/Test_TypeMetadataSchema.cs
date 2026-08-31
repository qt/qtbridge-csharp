// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Json.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public sealed class Test_TypeMetadataSchema
    {
        private const string TestUsings = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            """;

        private const string QmlModuleSource = """
            [assembly: Qt.Quick.QmlModule(Uri = "Application", IsRoot = true)]
            """;

        public TestContext TestContext { get; set; }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private static readonly string RepoRoot = FindRepoRoot();

        [TestMethod]
        public async Task LoadTimeTypeMetadata_ConformsToSchema()
        {
            var schema = LoadSchema();
            var json = await GenerateMetadataAsync();

            var result = schema.Evaluate(json, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            Assert.IsTrue(result.IsValid, Format(result));
        }

        [TestMethod]
        public async Task Dotted_Qml_Module_Uri_ConformsToSchema()
        {
            var schema = LoadSchema();
            var json = await GenerateMetadataAsync();

            json["types"]![0]!["qt"]!["qml"]!["module"] = "com.mycompany.qml.mymodule";

            var result = schema.Evaluate(json, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            Assert.IsTrue(result.IsValid, Format(result));
        }

        private static JsonSchema LoadSchema()
        {
            return JsonSchema.FromText(File.ReadAllText(Path.Combine(
                RepoRoot, "qt_bridge_metadata_schema.json")));
        }

        private async Task<JsonNode> GenerateMetadataAsync()
        {
            var ct = TestContext.CancellationTokenSource.Token;
            var manualTestDir = Path.Combine(RepoRoot, "tests", "manual", "MTest_DynamicObject");

            using var result = await TestCodeGenerator.GenerateAsync(
                sources: [
                    QmlModuleSource,
                    TestUsings + // BuildTimeType.cs relies on the manual project's implicit usings
                    await File.ReadAllTextAsync(Path.Combine(manualTestDir, "BuildTimeType.cs"), ct),
                    await File.ReadAllTextAsync(Path.Combine(manualTestDir, "LoadTimeType.cs"), ct)
                ],
                sourceRefs: [
                    typeof(Qt.ExportAttribute).Assembly, typeof(Qt.DotNet.Adapter).Assembly,
                    typeof(System.ComponentModel.INotifyPropertyChanged).Assembly,
                    typeof(Uri).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateType).Assembly
                ],
                ct: ct);
            Assert.IsTrue(result.Sink.Files.TryGetValue("qt_bridge_metadata.json", out var json),
                string.Join(", ", result.Sink.Files.Keys));
            return JsonNode.Parse(json)!;
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location) ?? "");
            while (dir != null) {
                if (File.Exists(Path.Combine(dir.FullName, "qtbridge-csharp.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root.");
        }

        private static string Format(EvaluationResults result) =>
            JsonSerializer.Serialize(result, SerializerOptions);
    }
}
