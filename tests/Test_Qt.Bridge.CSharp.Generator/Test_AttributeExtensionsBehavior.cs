// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt;
    using Qt.Bridge.CodeGeneration.Extensions;
    using Qt.Quick;
    using Support;

    [TestClass]
    public class Test_QmlElementAttributeBehavior
    {
        private const string MisspelledSource = """
            using Qt.Quick;
            namespace Test
            {
                [Qt.Quick.QmlElement(Singelton = true)]
                public class Foo { }
            }
        """;

        private const string MissingSource = """
            using Qt.Quick;
            namespace Test
            {
                [Qt.Quick.QmlElement()]
                public class Foo { }
            }
        """;

        private const string ValidSource = """
            using Qt.Quick;
            namespace Test
            {
                [Qt.Quick.QmlElement(Name = "Foo")]
                public class Foo { }
            }
        """;

        private static string SetupSource(string value) => $$"""
            using Qt.Quick;
            namespace Test
            {
                [Qt.Quick.QmlElement(Name = "{{value}}")]
                public class Foo { }
            }
         """;

        private static readonly Assembly AdapterAssembly = typeof(QmlElementAttribute).Assembly;

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task SourceWithMisspelledProperty_ShouldFailCompilation()
        {
            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                TestCodeGenerator.GenerateAsync(
                    [MisspelledSource],
                    sourceRefs: [AdapterAssembly],
                    ct: TestContext.CancellationTokenSource.Token)
            );

            Assert.Contains("Singelton", exception.Message);
        }

        [TestMethod]
        public async Task SourceWithMissingProperty_ShouldNotFailCompilation()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [MissingSource],
                sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            var foo = result.SourceAssembly.GetType("Test.Foo");
            Assert.IsNotNull(foo, $"Could not fetch {nameof(foo)} from source assembly.");

            var attr = foo.GetCustomAttributesData()
                .Single(a => a.AttributeType.Name == nameof(QmlElementAttribute));

            Assert.ThrowsExactly<ArgumentException>(() =>
                    (string)attr.Property(nameof(QmlElementAttribute.Name)),
                "Non-generic Property(...) shall throw on unknown property.");

            Assert.IsFalse(attr.TryProperty(nameof(QmlElementAttribute.Name), out var value),
                "Non-generic TryProperty(...) shall not throw on unknown property.");
            Assert.IsNull((string)value);
        }

        [TestMethod]
        public async Task SourceWithValidProperty_ShouldNotFailCompilation()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [ValidSource],
                sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            var foo = result.SourceAssembly.GetType("Test.Foo");
            Assert.IsNotNull(foo, $"Could not fetch {nameof(foo)} from source assembly.");

            var attr = foo.GetCustomAttributesData()
                .Single(a => a.AttributeType.Name == nameof(QmlElementAttribute));

            Assert.IsTrue(attr.TryProperty(nameof(QmlElementAttribute.Name), out var value));
            Assert.AreEqual("Foo", (string)value);

            var nonGeneric = (string)attr.Property(nameof(QmlElementAttribute.Name));
            Assert.AreEqual("Foo", nonGeneric);
        }

        [TestMethod,
            DataRow(""),
            DataRow(" Foo"),
            DataRow("Foo "),
            DataRow("Foo Bar"),
            DataRow("foo"),
            DataRow("_Foo"),
            DataRow("9Foo")
        ]
        public async Task InvalidQmlElementName_ShouldFailGeneration(string invalid)
        {
            var source = SetupSource(invalid);
            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                TestCodeGenerator.GenerateAsync([source],
                    sourceRefs: [typeof(QmlElementAttribute).Assembly],
                    ct: TestContext.CancellationTokenSource.Token)
            );

            Assert.Contains("QmlElement.Name", exception.Message);
            Assert.Contains("is invalid.", exception.Message);
        }

        [TestMethod,
            DataRow("Foo_"),
            DataRow("Foo_Bar"),
            DataRow("Foo1"),
            DataRow("Foo_1")
        ]
        public async Task ValidQmlElementName_WithUnderscoresOrNumIsAllowed(string value)
        {
            var source = SetupSource(value);
            using var result = await TestCodeGenerator.GenerateAsync(
                [source],
                sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.SourceAssembly.GetType("Test.Foo"));
        }

        [TestMethod]
        public async Task ValidName_Emits_QML_NAMED_ELEMENT()
        {
            const string src = """
                using Qt.Quick;
                namespace Test
                {
                    [Qt.Quick.QmlElement(Name = "Foo")]
                    public class Foo { }
                }
            """;

            using var result = await TestCodeGenerator.GenerateAsync(
                [src], sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/foo.h", out var hpp));
            Assert.Contains("QML_NAMED_ELEMENT(Foo)", hpp);
            Assert.DoesNotContain("QML_ELEMENT", hpp);
        }

        [TestMethod]
        public async Task MissingName_Emits_QML_ELEMENT()
        {
            const string src = """
                using Qt.Quick;
                namespace Test
                {
                    [Qt.Quick.QmlElement]
                    public class Foo { }
                }
            """;

            using var result = await TestCodeGenerator.GenerateAsync(
                [src], sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/foo.h", out var hpp));
            Assert.Contains("QML_ELEMENT", hpp);
            Assert.DoesNotContain("QML_NAMED_ELEMENT(", hpp);
        }

        [TestMethod]
        public async Task Singleton_Absent_DoesNotEmitMacro_Present_Does()
        {
            const string src = """
                using Qt.Quick;
                namespace Test
                {
                    [Qt.Quick.QmlElement(Name = "Foo")]
                    public class Foo { }

                    [Qt.Quick.QmlElement(Name = "Bar", Singleton = true)]
                    public class Bar { }
                }
            """;

            using var result = await TestCodeGenerator.GenerateAsync(
                [src], sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/foo.h", out var hpp));
            Assert.Contains("QML_NAMED_ELEMENT(Foo)", hpp);
            Assert.DoesNotContain("QML_SINGLETON", hpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/bar.h", out hpp));
            Assert.Contains("QML_NAMED_ELEMENT(Bar)", hpp);
            Assert.Contains("QML_SINGLETON", hpp);
        }

        private Regex LogParser { get; } = new(@"(?:^|\n)(\w+::\w+) --> ([^\r\n\s]*)(?:\r?\n|$)");

        private Dictionary<string, string> LogExportAs(string logText)
        {
            var types = LogParser.Matches(logText)
                .Where(m => m.Success && m.Groups.Count > 2)
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            Assert.IsNotEmpty(types);
            return types;
        }

        private async Task<(bool Metadata, bool SourceCode)> DefaultExportAs()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: [$$""""
                namespace Test
                {
                    public class MyDefault { }
                }
                """"]);
            var typeDefault = result.SourceAssembly.GetType("Test.MyDefault");
            Assert.IsNotNull(typeDefault);
            return (typeDefault.ExportAsMetadata(), typeDefault.ExportAsSourceCode());
        }

        [TestMethod]
        public async Task Export_Default_SameAs_NoConfig()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: [""""
                using Qt;
                namespace Test
                {
                    public class MyNoConfig { }

                    [Export(Options = ExportAs.Default)]
                    public class MyExportAsDefault { }
                }
                """"]);

            result.SelectedFiles = ["rules_log.txt"];
            var log = LogExportAs(result.CombinedText);
            Assert.Contains("Test::MyNoConfig", log.Keys);
            Assert.Contains("Test::MyExportAsDefault", log.Keys);
            Assert.AreEqual(log["Test::MyNoConfig"], log["Test::MyExportAsDefault"]);

            var typeNoConfig = result.SourceAssembly.GetType("Test.MyNoConfig");
            Assert.IsNotNull(typeNoConfig);

            var typeDefault = result.SourceAssembly.GetType("Test.MyExportAsDefault");
            Assert.IsNotNull(typeDefault);

            Assert.AreEqual(typeNoConfig.ExportAsMetadata(), typeDefault.ExportAsMetadata());
            Assert.AreEqual(typeNoConfig.ExportAsSourceCode(), typeDefault.ExportAsSourceCode());
        }

        [TestMethod]
        public async Task Export_Metadata_NotSameAs_SourceCode()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: [""""
                using Qt;
                namespace Test
                {
                    [Export(Options = ExportAs.Metadata)]
                    public class MyExportAsMetadata { }

                    [Export(Options = ExportAs.SourceCode)]
                    public class MyExportAsSourceCode { }
                }
                """"]);

            result.SelectedFiles = ["rules_log.txt"];
            var types = LogParser.Matches(result.CombinedText)
                .Where(m => m.Success && m.Groups.Count > 2)
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            Assert.IsNotEmpty(types);
            Assert.Contains("Test::MyExportAsMetadata", types.Keys);
            Assert.Contains("Test::MyExportAsSourceCode", types.Keys);
            Assert.AreNotEqual(
                types["Test::MyExportAsMetadata"], types["Test::MyExportAsSourceCode"]);

            var typeMetadata = result.SourceAssembly.GetType("Test.MyExportAsMetadata");
            Assert.IsNotNull(typeMetadata);
            Assert.IsTrue(typeMetadata.ExportAsMetadata());
            Assert.IsFalse(typeMetadata.ExportAsSourceCode());

            var typeSourceCode = result.SourceAssembly.GetType("Test.MyExportAsSourceCode");
            Assert.IsNotNull(typeSourceCode);
            Assert.IsTrue(typeSourceCode.ExportAsSourceCode());
            Assert.IsFalse(typeSourceCode.ExportAsMetadata());
        }

        [TestMethod,
            DataRow(nameof(ExportAs.Metadata)),
            DataRow(nameof(ExportAs.SourceCode))
        ]
        public async Task Assembly_Export_Is_Default(string exportAs)
        {
            var defaultExportAs = await DefaultExportAs();

            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: [$$""""
                using Qt;

                [assembly: Export(Options = ExportAs.{{exportAs}})]

                namespace Test
                {
                    public class MyNoConfig { }

                    [Export(Options = ExportAs.Default)]
                    public class MyExportAsDefault { }

                    [Export(Options = ExportAs.Metadata)]
                    public class MyExportAsMetadata { }

                    [Export(Options = ExportAs.SourceCode)]
                    public class MyExportAsSourceCode { }
                }
                """"]);

            bool assemblyAsMetadata = exportAs == nameof(ExportAs.Metadata);
            bool assemblyAsSourceCode = exportAs == nameof(ExportAs.SourceCode);

            var typeNoConfig = result.SourceAssembly.GetType("Test.MyNoConfig");
            Assert.IsNotNull(typeNoConfig);
            Assert.AreEqual(assemblyAsMetadata, typeNoConfig.ExportAsMetadata());
            Assert.AreEqual(assemblyAsSourceCode, typeNoConfig.ExportAsSourceCode());

            var typeDefault = result.SourceAssembly.GetType("Test.MyExportAsDefault");
            Assert.IsNotNull(typeDefault);
            Assert.AreEqual(defaultExportAs.Metadata, typeDefault.ExportAsMetadata());
            Assert.AreEqual(defaultExportAs.SourceCode, typeDefault.ExportAsSourceCode());

            var typeMetadata = result.SourceAssembly.GetType("Test.MyExportAsMetadata");
            Assert.IsNotNull(typeMetadata);
            Assert.IsTrue(typeMetadata.ExportAsMetadata());
            Assert.IsFalse(typeMetadata.ExportAsSourceCode());

            var typeSourceCode = result.SourceAssembly.GetType("Test.MyExportAsSourceCode");
            Assert.IsNotNull(typeSourceCode);
            Assert.IsTrue(typeSourceCode.ExportAsSourceCode());
            Assert.IsFalse(typeSourceCode.ExportAsMetadata());
        }

        [TestMethod,
            DataRow(""),
            DataRow("Global = true, ")]
        public async Task Assembly_GlobalExport_AppliesTo_AllTypes(string global)
        {
            var defaultExportAs = await DefaultExportAs();
            var exportAs = defaultExportAs.SourceCode ? "Metadata" : "SourceCode";

            using var result = await TestCodeGenerator.GenerateAsync(
                sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token,
                sources: [$$""""
                using Qt;

                [assembly: Export({{global}}Options = ExportAs.{{exportAs}})]

                namespace Test
                {
                    public class Foo
                    {
                        public int[] Bar { get; set; }
                    }
                }
                """"]);

            var typeFoo = result.SourceAssembly.GetType("Test.Foo");
            Assert.IsNotNull(typeFoo);
            Assert.AreNotEqual(defaultExportAs.Metadata, typeFoo.ExportAsMetadata());
            Assert.AreNotEqual(defaultExportAs.SourceCode, typeFoo.ExportAsSourceCode());

            var typeIntArray = result.Graph.TypeOf<int[]>();
            Assert.IsNotNull(typeIntArray);
            Assert.IsTrue(result.Graph.ContainsKey(typeIntArray));
            if (string.IsNullOrEmpty(global)) {
                Assert.AreEqual(defaultExportAs.Metadata, typeIntArray.ExportAsMetadata());
                Assert.AreEqual(defaultExportAs.SourceCode, typeIntArray.ExportAsSourceCode());
            } else {
                Assert.AreNotEqual(defaultExportAs.Metadata, typeIntArray.ExportAsMetadata());
                Assert.AreNotEqual(defaultExportAs.SourceCode, typeIntArray.ExportAsSourceCode());
            }
        }
    }
}
