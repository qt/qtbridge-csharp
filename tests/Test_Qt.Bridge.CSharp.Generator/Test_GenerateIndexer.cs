/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_GenerateIndexer
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task QDotNetFunction_TemplateArgs_Should_Not_PrefixStar()
        {
            const string source = """
            public sealed class Foo {}
            public sealed class Bar {}
            public class Subject
            {
               public int this[Foo k, Bar v] { get => 0; set {} }
            }
            """;

            var result = await TestCodeGenerator.GenerateAsync([source],
                ct: TestContext.CancellationTokenSource.Token);

            // Make sure the generator has emitted a header and source file
            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/hpp/subject.h", out _));
            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/cpp/subject.cpp", out var subject));

            // Make sure we do not emit prefixed template arguments
            var lines = subject.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            foreach (var line in lines) {
                Assert.DoesNotMatchRegex(
                    new Regex(@"(^|,)\s*\*\s*(?:QtDotNet::Global::)?(Foo|Bar)(\s|,|$|>)"),
                    line.Trim());
            }
        }
    }
}
