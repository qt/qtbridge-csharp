// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;
    using Qt.Quick;
    using MetadataRules = Qt.Bridge.CodeGeneration.Rules.Metadata;
    using SourceCodeRules = Qt.Bridge.CodeGeneration.Rules.SourceCode.Class;

    [TestClass]
    public class Test_GetType
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow("SourceCode", "public int getType() => 123;")]
        [DataRow("Metadata", "public int getType() => 123;")]
        [DataRow("SourceCode", "public int getType(int value) => value;")]
        [DataRow("Metadata", "public int getType(int value) => value;")]
        [DataRow("SourceCode", "public int GetType(int value) => value;")]
        [DataRow("Metadata", "public int GetType(int value) => value;")]
        [DataRow("SourceCode", "public string getType { get; set; }")]
        [DataRow("Metadata", "public string getType { get; set; }")]
        [DataRow("SourceCode", "public new string GetType { get; set; }")]
        [DataRow("Metadata", "public new string GetType { get; set; }")]
        [DataRow("SourceCode", "public int getType;")]
        [DataRow("SourceCode", "public new int GetType;")]
        [DataRow("SourceCode", "public event System.EventHandler GetType;")]
        [DataRow("Metadata", "public event System.EventHandler GetType;")]
        [DataRow("SourceCode", "public event System.EventHandler getType;")]
        [DataRow("Metadata", "public event System.EventHandler getType;")]
        public async Task UserGetTypeMemberIsSkippedWithWarning(string exportMode, string member)
        {
            using var result = await TestCodeGenerator.GenerateAsync([
                $$"""
                [Qt.Export(Options = Qt.ExportAs.{{exportMode}})]
                public class Foo
                {
                    {{member}}
                    public int Value { get; set; }
                }
                """],
                sourceRefs: [typeof(QmlElementAttribute).Assembly,
                    typeof(MetadataRules.GenerateType).Assembly,
                    typeof(SourceCodeRules.GenerateClass).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(Rules.Results.Any(rulesResult => rulesResult.Warning
                && rulesResult.Source?.ReflectedType?.Name == "Foo"
                && rulesResult.Message.Contains("Reserved name 'getType'")),
                "Skipping the user member must produce a diagnostic.");
            if (exportMode == "SourceCode") {
                var header = result.Sink.Files["source/hpp/foo.h"];
                Assert.Contains("Q_INVOKABLE QString getType() const;", header);
                Assert.DoesNotContain("Q_INVOKABLE qint32 getType(", header);
                Assert.DoesNotContain("Q_SIGNAL void getType(", header);
                Assert.DoesNotContain("Q_PROPERTY(QString getType", header);
                Assert.DoesNotContain("Q_PROPERTY(qint32 getType", header);
                Assert.Contains("Q_PROPERTY(qint32 value", header);
            } else {
                using var metadata =
                    JsonDocument.Parse(result.Sink.Files[TestCodeGenerator.MetadataFileName]);
                var type = metadata.RootElement.GetProperty("types").EnumerateArray()
                    .Single(type =>
                        type.GetProperty("dotNet").GetProperty("name").GetString() == "Foo");
                foreach (var collection in new[] { "methods", "properties", "events" }) {
                    if (!type.TryGetProperty(collection, out var members))
                        continue;
                    Assert.IsFalse(members.EnumerateArray().Any(member =>
                        member.GetProperty("qt").GetProperty("name").GetString() == "getType"));
                }
                Assert.IsTrue(type.GetProperty("properties").EnumerateArray().Any(property =>
                    property.GetProperty("qt").GetProperty("name").GetString() == "value"));
            }
        }

        [TestMethod]
        public async Task Hidden_GetType_Excludes_Type()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [
                    typeof(QmlElementAttribute).Assembly,
                    typeof(MetadataRules.GenerateType).Assembly,
                    typeof(SourceCodeRules.GenerateClass).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: ["""
                using Qt;

                namespace Test {
                    [Export(Options = ExportAs.Metadata)]
                    public class Foo { }

                    [Export(Options = ExportAs.SourceCode)]
                    public class Bar { }

                    [Export(Options = ExportAs.Metadata)]
                    public class Bolg
                    {
                        public new string GetType() => "BOLG";
                    }

                    [Export(Options = ExportAs.SourceCode)]
                    public class Snag
                    {
                        public new string GetType() => "SNAG";
                    }
                }
                """]);

            var graphTypes = result.Graph.NodeSet<Type>()
                .Select(type => type.ToString())
                .ToList();
            Assert.Contains("Test.Foo", graphTypes);
            Assert.Contains("Test.Bar", graphTypes);
            Assert.DoesNotContain("Test.Bolg", graphTypes);
            Assert.DoesNotContain("Test.Snag", graphTypes);
        }

        [TestMethod]
        public async Task Metadata_DoesNot_Export_GetType()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [
                    typeof(QmlElementAttribute).Assembly,
                    typeof(MetadataRules.GenerateType).Assembly,
                    typeof(SourceCodeRules.GenerateClass).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: ["""
                using Qt;

                namespace Test {
                    [Export(Options = ExportAs.Metadata)]
                    public class Foo { }
                }
                """]);

            Assert.Contains(TestCodeGenerator.MetadataFileName, result.Sink.Files.Keys);
            var metadata = JsonDocument
                .Parse(result.Sink.Files[TestCodeGenerator.MetadataFileName]);

            var methods = metadata.RootElement
                .GetProperty("types")
                .EnumerateArray()
                .SelectMany(type => type
                    .GetProperty("methods")
                    .EnumerateArray()
                    .Select(method => method
                        .GetProperty("qt"))
                    .Select(method => new
                    {
                        TypeName = type
                            .GetProperty("dotNet")
                            .GetProperty("name").GetString(),
                        MethodName = method
                            .GetProperty("name").GetString(),
                        ReturnType = method
                            .GetProperty("returnType").GetString(),
                        ParamCount = method
                            .TryGetProperty("parameters", out var args) ? args.GetArrayLength() : 0
                    }));
            Assert.DoesNotContain(method =>
                method.MethodName == "getType" && method.ParamCount == 0, methods);
        }

        [TestMethod]
        public async Task Source_Exports_GetType_Returning_String()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [
                    typeof(QmlElementAttribute).Assembly,
                    typeof(MetadataRules.GenerateType).Assembly,
                    typeof(SourceCodeRules.GenerateClass).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: ["""
                using Qt;
                namespace Test {
                    [Export(Options = ExportAs.SourceCode)]
                    public class Bar { }
                }
                """]);

            Assert.Contains("source/cpp/test/bar.cpp", result.Sink.Files.Keys);
            result.SelectedFiles = result.Sink.Files.Keys
                .Where(file => file.EndsWith(".cpp"))
                .ToList();

            Assert.Contains("\nQString Test::Bar::getType() const", result.CombinedText);
        }

        [TestMethod]
        [DataRow("SourceCode")]
        [DataRow("Metadata")]
        public async Task Generic_GetType_Overloads_Do_Not_Abort_Generation(string exportMode)
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [
                    typeof(QmlElementAttribute).Assembly,
                    typeof(MetadataRules.GenerateType).Assembly,
                    typeof(SourceCodeRules.GenerateClass).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: [$$"""
                using Qt;

                namespace Test {
                    [Export(Options = ExportAs.{{exportMode}})]
                    public class Foo { public int Value { get; set; } }

                    [Export(Options = ExportAs.{{exportMode}})]
                    public class GenericGetType
                    {
                        public T GetType<T>() => default;
                        public T GetType<T, U>() => default;
                    }
                }
                """]);

            // Handling an incompatible type must not abort generation for unrelated types.
            Assert.Contains("Test.Foo", [
                .. result.Graph.NodeSet<Type>().Select(type => type.ToString())
            ]);
        }
    }
}
