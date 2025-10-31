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
    }
}
