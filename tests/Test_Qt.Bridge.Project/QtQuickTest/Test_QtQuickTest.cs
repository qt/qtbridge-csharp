// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.QtQuickTest
{
    [TestClass]
    public class Test_QtQuickTest : ManagedTestBase
    {
        [TestMethod]
        public async Task QtQuickTest()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(@"QtQuickTest\main.cpp");
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", @"QtQuickTest\Program.cs");
                    project.CopyFile("tst_qtquicktest.qml", @"QtQuickTest\tst_qtquicktest.qml");
                });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_qtquicktest.qml")],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

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
