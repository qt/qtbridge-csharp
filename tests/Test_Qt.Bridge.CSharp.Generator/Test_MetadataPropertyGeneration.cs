// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_MetadataPropertyGeneration
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Metadata_Exports_Indexer_Accessors_As_Methods()
        {
            const string source = """
                using Qt;

                [assembly: Export(Options = ExportAs.Metadata)]

                namespace Test
                {
                    public class Properties
                    {
                        public int Value { get; set; }
                        public static int StaticValue { get; set; }
                        public int this[int index] { get => index; set { } }
                    }

                    public class StaticProperties
                    {
                        public static int StaticValue { get; set; }
                    }
                }
                """;

            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs:
                [
                    typeof(Qt.ExportAttribute).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateProperty).Assembly
                ],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("qt_bridge_metadata.json", out var json),
                string.Join(", ", result.Sink.Files.Keys));
            var types = JsonNode.Parse(json)!["types"]!.AsArray();

            var properties = Type(types, "Test.Properties")["properties"]!.AsArray();
            CollectionAssert.AreEqual(new[] { "Value" }, properties
                .Select(property => property!["dotNet"]!["name"]!.GetValue<string>()).ToArray());

            var methods = Type(types, "Test.Properties")["methods"]!.AsArray();
            AssertIndexerAccessor(methods, "get_Item", "item", "qint32");
            AssertIndexerAccessor(methods, "set_Item", "setItem", "qint32", "qint32");

            Assert.IsNull(Type(types, "Test.StaticProperties")["properties"]);
        }

        private static JsonNode Type(JsonArray types, string name)
            => types.Single(type => type!["dotNet"]!["name"]!.GetValue<string>() == name)!;

        private static void AssertIndexerAccessor(JsonArray methods, string dotNetName,
            string qtName, params string[] parameterTypes)
        {
            var method = methods
                .Single(method => method!["dotNet"]!["name"]!
                .GetValue<string>() == dotNetName)!;

            Assert.IsNotNull(method["dotNet"]!["metadataToken"]);
            Assert.AreEqual(qtName, method["qt"]!["name"]!.GetValue<string>());
            CollectionAssert.AreEqual(parameterTypes, method["qt"]!["parameters"]!
                .AsArray().Select(parameter => parameter!.GetValue<string>()).ToArray());
        }
    }
}
