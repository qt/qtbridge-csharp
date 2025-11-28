/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Test_Qt.DotNet.Project.Shared;

namespace Test_Qt.DotNet.Project.QtTest
{
    [TestClass]
    public class Test_QtTest : ManagedTestBase
    {
        [TestMethod]
        public async Task QtTest()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(@"QtTest\main.cpp");
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", @"QtTest\Program.cs");
                });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")]
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

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
