/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Test_Qt.DotNet.Project.QtQuickTest
{
    [TestClass]
    public class Test_QtQuickTest
    {
        [TestMethod]
        public async Task QtQuickTest()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", @"QtQuickTest\main.cpp")
                ]
            });
            temp.CopyFile("Program.cs", @"QtQuickTest\Program.cs");
            temp.CopyFile("tst_qtquicktest.qml", @"QtQuickTest\tst_qtquicktest.qml");

            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok);

            var run = await temp.RunAsync(new()
            {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_qtquicktest.qml")],
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });
            Assert.IsTrue(run.ExitCode == 0);
            var pass = "PASS   : Test_QtQuickTest::tst_qtquicktest::";
            Assert.Contains(pass + "initTestCase()", run.StdOut);
            Assert.Contains("Hello World from QML!", run.StdOut);
            Assert.Contains("Hello World from C#!", run.StdOut);
            Assert.Contains("Hello World from C++!", run.StdOut);
            Assert.Contains(pass + "test_fortytwo()", run.StdOut);
            Assert.Contains(pass + "cleanupTestCase()", run.StdOut);
        }
    }
}
