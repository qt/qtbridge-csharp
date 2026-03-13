// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.AdapterInit
{
    [TestClass]
    public class Test_AdapterInit : ManagedTestBase
    {
        [TestMethod]
        public async Task AdapterInterop()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine("AdapterInit", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("AdapterInit", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_AdapterInit::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_AdapterInit::dotnetMain()", run.StdOut);
            Assert.Contains("AdapterInit managed app ready", run.StdOut);
            Assert.Contains("PASS   : Test_AdapterInit::initAdapter()", run.StdOut);
            Assert.Contains("AdapterInit native host ready", run.StdOut);
            Assert.Contains("PASS   : Test_AdapterInit::callManagedStaticProperty()", run.StdOut);
            Assert.Contains("PASS   : Test_AdapterInit::callStaticMethod()", run.StdOut);
            Assert.Contains("PASS   : Test_AdapterInit::callInstanceMethod()", run.StdOut);
            Assert.Contains("PASS   : Test_AdapterInit::cleanupTestCase()", run.StdOut);
        }
    }
}
