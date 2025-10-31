/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Generator
{
    using Qt.DotNet.Extensions;
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
            var result = await TestCodeGenerator.GenerateAsync(
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
            var result = await TestCodeGenerator.GenerateAsync(
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
            var result = await TestCodeGenerator.GenerateAsync(
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

            var result = await TestCodeGenerator.GenerateAsync(
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

            var result = await TestCodeGenerator.GenerateAsync(
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

            var result = await TestCodeGenerator.GenerateAsync(
                [src], sourceRefs: [typeof(QmlElementAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/foo.h", out var hpp));
            Assert.Contains("QML_NAMED_ELEMENT(Foo)", hpp);
            Assert.DoesNotContain("QML_SINGLETON", hpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/bar.h", out hpp));
            Assert.Contains("QML_NAMED_ELEMENT(Bar)", hpp);
            Assert.Contains("QML_SINGLETON", hpp);
        }
    }
}
