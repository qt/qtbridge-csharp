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
    public class Test_MetadataSingletonGeneration
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Metadata_Records_Whether_A_Qml_Element_Is_A_Singleton()
        {
            const string source = """
                using System.ComponentModel;
                using Qt;
                using Qt.Bridge.Models;
                using Qt.Quick;

                [assembly: Export(Options = ExportAs.Metadata)]

                namespace Test
                {
                    [QmlElement(Singleton = true)]
                    public sealed class PlainSingleton
                    {
                        public int Value { get; set; }
                        public int Compute(int x) => x;
                    }

                    [QmlElement]
                    public sealed class PlainElement
                    {
                        public int Value { get; set; }
                    }

                    [QmlElement(Name = "Renamed", Singleton = true)]
                    public sealed class RenamedSingleton
                    {
                        public int Value { get; set; }
                    }

                    [QmlElement(Singleton = true)]
                    public sealed class ModelSingleton : ListModel<int>
                    {
                        public int Value { get; set; }
                        public override int ItemCount() => 0;
                        public override int Data(int index) => index;
                    }

                    public sealed class Unattributed
                    {
                        public int Value { get; set; }
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

            // A singleton without a bridge base class is the case that used to
            // register as an ordinary type, leaving QML with no instance.
            AssertSingleton(types, "Test.PlainSingleton", "PlainSingleton", true);

            // An ordinary element must stay instantiable.
            AssertSingleton(types, "Test.PlainElement", "PlainElement", false);

            // The flag is independent of an explicit QML name.
            AssertSingleton(types, "Test.RenamedSingleton", "Renamed", true);

            // A model-derived singleton keeps both the flag and its model block.
            var model = AssertSingleton(types, "Test.ModelSingleton", "ModelSingleton", true);
            Assert.IsNotNull(model["qt"]!["model"],
                "a model-derived singleton must still be described as a model");

            // A type carrying no attribute is exported all the same, and must
            // not become a singleton by omission.
            AssertSingleton(types, "Test.Unattributed", "Unattributed", false);
        }

        private static JsonNode AssertSingleton(JsonArray types, string dotNetName, string qmlName,
            bool expected)
        {
            var type = Type(types, dotNetName);
            var qml = type["qt"]!["qml"];
            Assert.IsNotNull(qml, $"{dotNetName} is missing its qml block");
            Assert.AreEqual(qmlName, qml!["name"]!.GetValue<string>());
            Assert.IsNotNull(qml["singleton"], $"{dotNetName} is missing the singleton flag");
            Assert.AreEqual(expected, qml["singleton"]!.GetValue<bool>(),
                $"{dotNetName} has the wrong singleton flag");
            return type;
        }

        private static JsonNode Type(JsonArray types, string name)
            => types.Single(type => type!["dotNet"]!["name"]!.GetValue<string>() == name)!;
    }
}
