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
    public class Test_ExportModeParity
    {
        public TestContext TestContext { get; set; }

        // Names that C++ cannot use as they are, acronyms, and single letters.
        private const string Source = """
            using System;
            using System.ComponentModel;

            namespace Test
            {
                public enum Shade { Red = 0, Green = -1, _Hidden = 5, Default = 7 }

                [Qt.Quick.QmlElement]
                public class Library : INotifyPropertyChanged
                {
                    public event PropertyChangedEventHandler PropertyChanged;
                    public event EventHandler Reloaded;
                    public event EventHandler URLChanged;
                    public event EventHandler Delete;

                    public string Title { get; set; }
                    public int ID { get; }
                    public string URLPath { get; set; }
                    public int New { get; set; }
                    public int _Count { get; set; }
                    public int X { get; set; }
                    public Shade Shade { get; set; }

                    public string GetHTTPResponse(string url, int retries) => url;
                    public bool IsOK() => true;
                    public void Switch() { }
                    public void _Reset() { }
                    public void A() { }
                    public int this[int index] { get => index; set { } }
                }
            }
            """;

        // The same C# code must present the same API to QML whether its types are exported
        // as source code or as type metadata, so that QML code works unchanged with either.

        [TestMethod]
        public async Task Metadata_PresentsSameQmlApiAsSourceCode()
        {
            var source = await SourceCodeApi(TestContext, Source, "library.h");
            var metadata = await MetadataApi(TestContext, Source, "Test.Library");

            CollectionAssert.AreEquivalent(source, metadata, Difference(source, metadata));
        }

        [TestMethod]
        public async Task Metadata_PresentsSameEnumAsSourceCode()
        {
            var source = await SourceCodeApi(TestContext, Source, "shade.h");
            var metadata = await MetadataApi(TestContext, Source, "Test.Shade");

            CollectionAssert.AreEquivalent(source.Where(e => e.StartsWith("enum")).ToArray(),
                metadata.Where(e => e.StartsWith("enum")).ToArray());
        }

        internal static string Difference(string[] source, string[] metadata)
        {
            return $"\nOnly in source code:\n  {string.Join("\n  ", source.Except(metadata))}"
                + $"\nOnly in metadata:\n  {string.Join("\n  ", metadata.Except(source))}";
        }

        internal static async Task<string[]> SourceCodeApi(TestContext context, string source,
            string header)
        {
            return QmlApi.FromSourceCode(await SourceCodeHeader(context, source, header));
        }

        internal static async Task<string> SourceCodeHeader(TestContext context, string source,
            string header)
        {
            using var result = await Generate(context, Qt.ExportAs.SourceCode, source);
            Assert.IsTrue(result.Sink.Files.TryGetValue($"source/hpp/test/{header}", out var hpp),
                string.Join(", ", result.Sink.Files.Keys));
            return hpp;
        }

        internal static async Task<string[]> MetadataApi(TestContext context, string source,
            string typeName)
        {
            return QmlApi.FromMetadata(await MetadataType(context, source, typeName));
        }

        internal static async Task<JsonNode> MetadataType(TestContext context, string source,
            string typeName)
        {
            using var result = await Generate(context, Qt.ExportAs.Metadata, source);
            Assert.IsTrue(result.Sink.Files.TryGetValue(TestCodeGenerator.MetadataFileName,
                out var json), string.Join(", ", result.Sink.Files.Keys));
            return JsonNode.Parse(json)!["types"]!.AsArray()
                .Single(t => t!["dotNet"]!["name"]!.GetValue<string>() == typeName)!;
        }

        internal static Task<TestCodeGenerator.Result> Generate(TestContext context,
            Qt.Options exportAs, string source)
        {
            return TestCodeGenerator.GenerateAsync([source],
                sourceRefs:
                [
                    typeof(Qt.ExportAttribute).Assembly,
                    typeof(System.ComponentModel.INotifyPropertyChanged).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateProperty).Assembly
                ],
                defaultExportOptions: exportAs,
                ct: context.CancellationTokenSource.Token);
        }
    }
}
