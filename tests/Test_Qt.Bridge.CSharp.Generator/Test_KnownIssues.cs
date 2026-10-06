// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;
    using static Test_ExportModeParity;

    /// <summary>
    /// Known problems that are not fixed yet; see <see cref="KnownIssue"/>. Each test checks the
    /// desired behavior and ends inconclusive while the problem remains.
    /// </summary>
    [TestClass]
    [TestCategory(KnownIssue.Category)]
    public class Test_KnownIssues
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Metadata_ExportsFieldsLikeSourceCode()
        {
            const string source = """
                namespace Test
                {
                    public class Members
                    {
                        public int Field;
                    }
                }
                """;

            var sourceApi = await SourceCodeApi(TestContext, source, "members.h");
            var metadataApi = await MetadataApi(TestContext, source, "Test.Members");

            Assert.Contains("property field writable", sourceApi);
            KnownIssue.Check(metadataApi.Contains("property field writable"),
                "Public fields are exported to QML as properties by source code exports, but not "
                + "by type metadata exports.",
                Difference(sourceApi, metadataApi),
                "Export fields in the metadata rules (Qt.Bridge.CSharp.Generator.Rules.Metadata) "
                + "and the schema, and let the native host access them; metadata_loader.cpp and "
                + "QDotNetDynamicObject only define properties, methods and events so far.");
        }

        [TestMethod]
        public async Task Metadata_ExportsDelegatePropertiesLikeSourceCode()
        {
            const string source = """
                using System;

                namespace Test
                {
                    public class Members
                    {
                        public Func<int, int> Transform { get; set; }
                    }
                }
                """;

            var sourceApi = await SourceCodeApi(TestContext, source, "members.h");
            var metadataApi = await MetadataApi(TestContext, source, "Test.Members");

            KnownIssue.Check(sourceApi.Order().SequenceEqual(metadataApi.Order()),
                "Delegate properties are exported by source code exports as a writable QJSValue "
                + "property plus an invoke<Name>() method, by type metadata exports as a QVariant "
                + "property without invoke method.",
                Difference(sourceApi, metadataApi),
                "Export delegate properties in the metadata rules like GenerateProperty of the "
                + "source code rules does, and support QJSValue properties and invoking delegates "
                + "in QDotNetDynamicObject.");
        }

        [TestMethod]
        public async Task Metadata_ExportsPropertyTypesLikeSourceCode()
        {
            const string source = """
                namespace Test
                {
                    public class Book { }

                    public class Shelf
                    {
                        public Book Favorite { get; set; }
                        public Book Pick(Book book) => book;
                    }
                }
                """;

            var header = await SourceCodeHeader(TestContext, source, "shelf.h");
            var pick = Regex.Match(header,
                @"Q_INVOKABLE (?<return>[^(]*?)\s*\bpick\((?<parameter>[^)]*?)\s*\bbook\)");
            var sourceTypes = new[]
            {
                ("property", QmlApi.SourceCodeProperties(header)
                    .Single(p => p.Name == "favorite").Type),
                ("return value", pick.Groups["return"].Value),
                ("parameter", pick.Groups["parameter"].Value)
            };

            var type = await MetadataType(TestContext, source, "Test.Shelf");
            var method = type["methods"]!.AsArray()
                .Single(m => m!["dotNet"]!["name"]!.GetValue<string>() == "Pick")!["qt"]!;
            var metadataTypes = new[]
            {
                ("property", QmlApi.MetadataProperties(type)
                    .Single(p => p.Name == "favorite").Type),
                ("return value", method["returnType"]!.GetValue<string>()),
                // A parameter is its type, or an object with name and type.
                ("parameter", method["parameters"]![0] is JsonObject parameter
                    ? parameter["type"]!.GetValue<string>()
                    : method["parameters"]![0]!.GetValue<string>())
            };
            var untyped = metadataTypes.Where(entry => entry.Item2 == "QVariant")
                .Select(entry => entry.Item1).ToArray();

            Assert.IsTrue(pick.Success, header);
            KnownIssue.Check(untyped.Length == 0,
                "Properties, parameters and return values of exported class types are typed by "
                + "source code exports, but are QVariant in type metadata exports, so QML tooling "
                + "and typed QML properties do not know their type.",
                string.Join("; ", sourceTypes.Zip(metadataTypes).Select(entry =>
                    $"{entry.First.Item1}: source code type '{entry.First.Item2}', type "
                    + $"metadata type '{entry.Second.Item2}'")),
                "Allow exported types as types in the metadata schema, write them in the metadata "
                + "rules (MetadataCompatibleType() maps them to object), and let "
                + "metadata_loader.cpp register properties and methods with the type's "
                + "meta-object.");
        }

        [TestMethod]
        public async Task SourceCode_NamesDoNotCollideWithInternalIdentifiers()
        {
            const string source = """
                namespace Test
                {
                    public class Short
                    {
                        public int D { get; set; }
                        public int I() => 0;
                        public int Mix(int result, int callback, int method, int value, int d)
                            => 0;
                    }
                }
                """;

            using var result = await Generate(TestContext, Qt.ExportAs.SourceCode, source);
            var code = result.Sink.Files["source/hpp/test/short.h"]
                + result.Sink.Files["source/cpp/test/short.cpp"];

            // Identifiers of the generated code: members, local variables and helper functions.
            var internalNames = Regex.Matches(code,
                    @"\w+(?:Private|Init) \*(?<name>\w+) = nullptr"
                    + @"|\bauto (?<name>\w+) =|\b(?<name>method|constructor)[(<]")
                .Select(match => match.Groups["name"].Value)
                .ToHashSet();
            // Names taken from C#: properties, methods and their parameters, each with where it
            // is used, so that the same name used as property and as parameter counts twice.
            var exportedNames = QmlApi.SourceCodeProperties(code)
                .Select(p => (p.Name, Label: $"property {p.Name}"))
                .Concat(Regex.Matches(code, @"Q_INVOKABLE [^(]*?\b(?<name>\w+)\((?<args>[^)]*)\)")
                    .SelectMany(match => match.Groups["args"].Value.Split(',')
                        .Select(arg => arg.Trim().Split(' ').Last())
                        .Select(arg => (Name: arg,
                            Label: $"parameter {arg} of {match.Groups["name"].Value}"))
                        .Append((Name: match.Groups["name"].Value,
                            Label: $"method {match.Groups["name"].Value}"))))
                .Where(entry => entry.Name.Length > 0)
                .Distinct();
            var collisions = exportedNames.Where(entry => internalNames.Contains(entry.Name))
                .Select(entry => entry.Label)
                .Order()
                .ToArray();

            KnownIssue.Check(collisions.Length == 0,
                "Names taken from C# can be identical to identifiers of the generated C++ code, "
                + "which then does not compile: a property D or method I() collides with the d/i "
                + "members, a parameter d hides the d member, a parameter result redeclares the "
                + "local result, and a parameter method hides the method() helper.",
                $"colliding names: {string.Join(", ", collisions)}",
                "Give the generator's own identifiers names that C# names cannot produce, for "
                + "example with a reserved prefix, in the source code rules and the static "
                + "sources (Qt.Bridge.CSharp.Generator.Rules.SourceCode). Do not rename the C# "
                + "names, since type metadata exports must keep the same QML names.");
        }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task EnumKeys_AreAccessibleThroughTheirType(string exportAs)
        {
            const string source = """
                namespace Test
                {
                    public enum Mode { Upper, _Hidden, lower }

                    public class Device
                    {
                        public Mode Mode { get; set; }
                    }
                }
                """;

            var api = exportAs == "source"
                ? await SourceCodeApi(TestContext, source, "mode.h")
                : await MetadataApi(TestContext, source, "Test.Mode");
            var keys = api.Where(entry => entry.StartsWith("enum "))
                .Select(entry => entry.Split(' ')[1])
                .ToArray();
            // The QML engine looks up Type.key only for keys starting with an upper-case letter
            // (QQmlTypeWrapper::virtualGet); Type.Values.key has no such restriction.
            var notThroughType = keys.Where(key => !char.IsAsciiLetterUpper(key[0])).ToArray();

            Assert.HasCount(3, keys);
            KnownIssue.Check(notThroughType.Length == 0,
                "C# enum values starting with an underscore (exported as z_...) or a lower-case "
                + "letter are not accessible as Type.key in QML, because the QML engine only "
                + "looks up keys starting with an upper-case letter there. They are accessible "
                + "through the enum, as Type.Values.key, in either export mode.",
                $"keys not accessible as Type.key: {string.Join(", ", notThroughType)}",
                "Design decision: either document Type.Values.key for such keys, or export them "
                + "under names starting with an upper-case letter in the Enums meta function "
                + "(Qt.Bridge.CSharp.Generator.MetaFunctions), which both export modes use, at "
                + "the cost of QML names that differ more from the C# names.");
        }

        // The meta-object lists properties in the generated order: the order of the Q_PROPERTY
        // declarations, or of the type metadata properties that metadata_loader.cpp adds.
        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task Properties_KeepDeclarationOrder(string exportAs)
        {
            const string source = """
                namespace Test
                {
                    public class Ordered
                    {
                        public int Zeta { get; set; }
                        public int Alpha { get; set; }
                        public int Middle { get; set; }
                    }
                }
                """;

            var order = (exportAs == "source"
                    ? QmlApi.SourceCodeProperties(
                        await SourceCodeHeader(TestContext, source, "ordered.h"))
                    : QmlApi.MetadataProperties(
                        await MetadataType(TestContext, source, "Test.Ordered")))
                .Select(p => p.Name)
                .Where(name => name != "nestedQmlElements")
                .ToArray();

            Assert.HasCount(3, order);
            KnownIssue.Check(order.SequenceEqual(["zeta", "alpha", "middle"]),
                "Properties reach QML in alphabetical order, not in the order they are declared "
                + "in C#, so generic forms and grids built by iterating over an object's "
                + "properties show them in the wrong order.",
                $"property order: {string.Join(", ", order)}",
                "The generator sorts the properties of a type by name (Sorted = true for the "
                + "property placeholders in the source code and metadata rules). Keep the "
                + "declaration order, which reflection reports as metadata token order.");
        }
    }
}
