/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Generator
{
    using Support;

    [TestClass]
    public class Test_FnVarNames
    {
        public TestContext TestContext { get; set; }

        private static string ExtractFn(string combined, string baseName)
        {
            var m = Regex.Match(combined, $@"\bfn{baseName}_[0-9A-F]+\b");
            return m.Success ? m.Value : null;
        }

        [TestMethod]
        public async Task FnVarNames()
        {
            var v1 = new[]
            {
                """
                public class A
                {
                    public void Alpha() { } // earlier type; stable here
                }
                """,
                """
                public class Foo
                {
                    public void Target(int x) { } // <-- supposed the stay unchanged across versions
                }
                """
            };

            var v2 = new[]
            {
                """
                public class A
                {
                    public void Alpha() { }
                    public void NewOne() { } // <-- only change
                }
                """,
                """
                public class Foo
                {
                    public void Target(int x) { } // unchanged, but its number shifts
                }
                """
            };

            var r1 = await TestCodeGenerator.GenerateAsync(v1);
            Assert.IsTrue(r1.Sink.Files.TryGetValue("source/cpp/foo.cpp", out var r1Cpp));

            var r2 = await TestCodeGenerator.GenerateAsync(v2);
            Assert.IsTrue(r2.Sink.Files.TryGetValue("source/cpp/foo.cpp", out var r2Cpp));

            var id1 = ExtractFn(r1Cpp, "Target");
            var id2 = ExtractFn(r2Cpp, "Target");

            Assert.IsNotNull(id1, "Expected to find fnTarget_* in v1 output.");
            Assert.IsNotNull(id2, "Expected to find fnTarget_* in v2 output.");

            Assert.AreEqual(id1, id2, "Unchanged method should produced equal fn name.");
        }
    }
}
