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
    public class Test_MetadataMethodGeneration
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task ExactOverloads_AreExportedAlongsideAssignableOverloads()
        {
            const string source = """
                using System;
                namespace Test
                {
                    public class Collection
                    {
                        public void Add(string value) { }
                        public int Add(object value) => 0;
                        public bool Contains(string value) => false;
                        public bool Contains(object value) => false;
                        public void CopyTo(string[] array, int index) { }
                        public void CopyTo(object array, int index) { }
                        public int IndexOf(string value) => 0;
                        public int IndexOf(object value) => 0;
                        public void Insert(int index, string value) { }
                        public void Insert(int index, object value) { }
                        public bool Remove(string value) => false;
                        public void Remove(object value) { }
                    }
                }
                """;
            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs: [typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateMethod).Assembly],
                defaultExportOptions: Qt.ExportAs.Metadata,
                ct: TestContext.CancellationTokenSource.Token);
            var types = JsonNode.Parse(result.Sink.Files[TestCodeGenerator.MetadataFileName])!["types"]!
                .AsArray();
            var methods = types.Single(type => type!["dotNet"]!["name"]!.GetValue<string>()
                == "Test.Collection")!["methods"]!.AsArray();
            string[] names = ["Add", "Contains", "CopyTo", "IndexOf", "Insert", "Remove"];
            var expectedTokens = result.SourceAssembly.GetType("Test.Collection")!.GetMethods()
                .Where(method => names.Contains(method.Name)).Select(method => method.MetadataToken)
                .Order().ToArray();
            var actualTokens = methods.Where(method => names.Contains(
                    method!["dotNet"]!["name"]!.GetValue<string>()))
                .Select(method => method!["dotNet"]!["metadataToken"]!.GetValue<int>())
                .Order().ToArray();
            CollectionAssert.AreEqual(expectedTokens, actualTokens);
        }

        [TestMethod]
        public async Task EnumConversions_StillRejectAmbiguityAndPreferExactInt32()
        {
            const string source = """
                namespace Test
                {
                    public enum Color { Red, Blue }
                    public enum Size { Small, Large }
                    public class Methods
                    {
                        public void Choose(Color value) { }
                        public void Choose(Size value) { }
                        public void Select(Color value) { }
                        public void Select(int value) { }
                    }
                }
                """;
            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs: [typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateMethod).Assembly],
                defaultExportOptions: Qt.ExportAs.Metadata,
                ct: TestContext.CancellationTokenSource.Token);
            var types = JsonNode.Parse(result.Sink.Files[TestCodeGenerator.MetadataFileName])!["types"]!
                .AsArray();
            var methods = types.Single(type => type!["dotNet"]!["name"]!.GetValue<string>()
                == "Test.Methods")!["methods"]!.AsArray();
            Assert.IsFalse(methods.Any(method => method!["dotNet"]!["name"]!.GetValue<string>()
                == "Choose"));
            var select = methods.Single(method => method!["dotNet"]!["name"]!.GetValue<string>()
                == "Select")!;
            var expected = result.SourceAssembly.GetType("Test.Methods")!.GetMethods()
                .Single(method => method.Name == "Select"
                    && method.GetParameters()[0].ParameterType.FullName == "System.Int32");
            Assert.AreEqual(expected.MetadataToken, select["dotNet"]!["metadataToken"]!.GetValue<int>());
        }
    }
}
