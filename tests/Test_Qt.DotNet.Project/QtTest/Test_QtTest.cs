/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Test_Qt.DotNet.Project.QtTest
{
    [TestClass]
    public class Test_QtTest
    {
        [TestMethod]
        public async Task QtTest()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", @"QtTest\main.cpp")
                ]
            });
            temp.CopyFile("Program.cs", @"QtTest\Program.cs");

            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok);

            var run = await temp.RunAsync(new()
            {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")]
            });
            Assert.IsTrue(run.ExitCode == 0);
            Assert.Contains("PASS   : Test_QtTest::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_QtTest::assemblyExists()", run.StdOut);
            Assert.Contains("PASS   : Test_QtTest::dotnetMain()", run.StdOut);
            Assert.Contains("Hello World from C++!", run.StdOut);
            Assert.Contains("Hello World from C#!", run.StdOut);
            Assert.Contains("PASS   : Test_QtTest::initAdapter()", run.StdOut);
            Assert.Contains("PASS   : Test_QtTest::callStatic()", run.StdOut);
            Assert.Contains("PASS   : Test_QtTest::cleanupTestCase()", run.StdOut);
        }
    }
}
