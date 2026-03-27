// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_QmlModule
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Project_Root_Without_Qml_Files()
        {
            const string source = """
            [assembly: Qt.Quick.QmlFile(
                Uri = "MyModule", TypeName = "Main", IsRoot = false,
                Path = @"MyModule\Main.qml")]

            public class Foo { public int Bar { get; set; } }
            """;

            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs: [typeof(Qt.Quick.QmlFileAttribute).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            // Make sure the generator has emitted a header and source file
            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/hpp/foo.h", out _));
            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/cpp/foo.cpp", out _));
        }
    }
}
