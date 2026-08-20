// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    [TestClass]
    public sealed class Test_TypeMetadataSchema
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private static readonly string RepoRoot = FindRepoRoot();

        [TestMethod]
        public void MetadataFixture_ConformsToSchema()
        {
            var schema = LoadSchema();
            var json = LoadManualSampleMetadataJson();

            var result = schema.Evaluate(json, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            Assert.IsTrue(result.IsValid, Format(result));
        }

        [TestMethod]
        public void Dotted_Qml_Module_Uri_ConformsToSchema()
        {
            var schema = LoadSchema();
            var json = LoadManualSampleMetadataJson();

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

        private static JsonNode LoadManualSampleMetadataJson()
        {
            return JsonNode.Parse(File.ReadAllText(Path.Combine(
                RepoRoot, "tests", "manual", "MTest_DynamicObject", "qt_bridge_metadata.json")))!;
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
