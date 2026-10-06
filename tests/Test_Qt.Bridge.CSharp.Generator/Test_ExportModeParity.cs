// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
            var source = await SourceCodeApi("library.h");
            var metadata = await MetadataApi("Test.Library");

            CollectionAssert.AreEquivalent(source, metadata,
                $"\nOnly in source code:\n  {string.Join("\n  ", source.Except(metadata))}"
                + $"\nOnly in metadata:\n  {string.Join("\n  ", metadata.Except(source))}");
        }

        [TestMethod]
        public async Task Metadata_PresentsSameEnumAsSourceCode()
        {
            var source = await SourceCodeApi("shade.h");
            var metadata = await MetadataApi("Test.Shade");

            CollectionAssert.AreEquivalent(source.Where(e => e.StartsWith("enum")).ToArray(),
                metadata.Where(e => e.StartsWith("enum")).ToArray());
        }

        private async Task<string[]> SourceCodeApi(string header, string source = Source)
        {
            using var result = await Generate(Qt.ExportAs.SourceCode, source);
            Assert.IsTrue(result.Sink.Files.TryGetValue($"source/hpp/test/{header}", out var hpp),
                string.Join(", ", result.Sink.Files.Keys));

            var api = new List<string>();
            foreach (Match property in Regex.Matches(hpp, @"Q_PROPERTY\((?<decl>[^)]*)\)")) {
                var tokens = property.Groups["decl"].Value
                    .Split((char[])[' ', '\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries);
                var accessor = System.Array.FindIndex(tokens,
                    token => token is "READ" or "WRITE" or "MEMBER");
                var name = tokens[accessor - 1].TrimStart('*');
                var notify = System.Array.IndexOf(tokens, "NOTIFY");
                api.Add($"property {name} "
                    + (tokens.Contains("WRITE") ? "writable" : "readonly")
                    + (notify < 0 ? "" : $" notify {tokens[notify + 1]}"));
            }

            foreach (Match method in Regex.Matches(hpp,
                @"Q_INVOKABLE [^(]*?\b(?<name>\w+)\((?<args>[^)]*)\)")) {
                var arity = method.Groups["args"].Value
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries).Length;
                api.Add($"method {method.Groups["name"].Value}/{arity}");
            }

            foreach (Match signal in Regex.Matches(hpp, @"Q_SIGNAL void (?<name>\w+)\("))
                api.Add($"signal {signal.Groups["name"].Value}");

            foreach (Match entry in Regex.Matches(
                Regex.Match(hpp, @"enum Values\s*\{(?<body>[^}]*)\}").Groups["body"].Value,
                @"(?<key>\w+)\s*=\s*(?<value>-?\d+)")) {
                api.Add($"enum {entry.Groups["key"].Value} = {entry.Groups["value"].Value}");
            }

            return [.. api.Distinct()];
        }

        // Applies the rules by which metadata_loader.cpp and QDotNetDynamicObject build the
        // metaobject from type metadata at run time.
        private async Task<string[]> MetadataApi(string typeName, string source = Source)
        {
            using var result = await Generate(Qt.ExportAs.Metadata, source);
            Assert.IsTrue(result.Sink.Files.TryGetValue(TestCodeGenerator.MetadataFileName,
                out var json), string.Join(", ", result.Sink.Files.Keys));
            var type = JsonNode.Parse(json)!["types"]!.AsArray()
                .Single(t => t!["dotNet"]!["name"]!.GetValue<string>() == typeName)!;

            static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];
            static string QtName(JsonNode qtName, JsonNode dotNet) =>
                qtName?.GetValue<string>() ?? Camel(dotNet["name"]!.GetValue<string>());

            var api = new List<string>();
            if (type["qt"]!["isQmlElement"]?.GetValue<bool>() == true)
                api.Add("property nestedQmlElements readonly");

            foreach (var property in type["properties"]?.AsArray() ?? []) {
                var dotNet = property!["dotNet"]!;
                var name = QtName(property["qt"]!["name"], dotNet);
                var notify = dotNet["isNotifiable"]?.GetValue<bool>() == false ? null
                    : property["qt"]!["notify"]?.GetValue<string>() ?? $"{name}Changed";
                api.Add($"property {name} "
                    + (dotNet["hasSet"]?.GetValue<bool>() == false ? "readonly" : "writable")
                    + (notify == null ? "" : $" notify {notify}"));
                if (notify != null)
                    api.Add($"signal {notify}");
            }

            foreach (var ev in type["events"]?.AsArray() ?? [])
                api.Add($"signal {QtName(ev!["qt"]?["signal"], ev["dotNet"]!)}");

            foreach (var method in type["methods"]?.AsArray() ?? []) {
                var arity = method!["qt"]!["parameters"]?.AsArray().Count ?? 0;
                api.Add($"method {QtName(method["qt"]!["name"], method["dotNet"]!)}/{arity}");
            }

            api.AddRange(["method toString/0", "method equals/1", "method asDotNetObject/0"]);

            foreach (var entry in type["qt"]!["enum"]?.AsObject() ?? [])
                api.Add($"enum {entry.Key} = {entry.Value!.GetValue<int>()}");

            return [.. api.Distinct()];
        }

        private Task<TestCodeGenerator.Result> Generate(Qt.Options exportAs, string source)
        {
            return TestCodeGenerator.GenerateAsync([source],
                sourceRefs:
                [
                    typeof(Qt.ExportAttribute).Assembly,
                    typeof(System.ComponentModel.INotifyPropertyChanged).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateProperty).Assembly
                ],
                defaultExportOptions: exportAs,
                ct: TestContext.CancellationTokenSource.Token);
        }
    }
}
