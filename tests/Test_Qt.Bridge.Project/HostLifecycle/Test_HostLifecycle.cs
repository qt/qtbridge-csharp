// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.HostLifecycle
{
    [TestClass]
    public class Test_HostLifecycle : ManagedTestBase
    {
        [TestMethod]
        public async Task Host_Load_Run_And_Unload()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine("HostLifecycle", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("HostLifecycle", "Program.cs"));
            });

            var hostRun = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                Args = ["loadHost", "runtimeProperties", "unloadHost"],
                StdErr = Redirect.StdOut
            });

            if (!string.IsNullOrWhiteSpace(hostRun.StdOut))
                Console.WriteLine(hostRun.StdOut);

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, hostRun.ExitCode,
                ExitCodeHelper.ToString(hostRun.ExitCode));

            Assert.Contains("PASS   : Test_HostLifecycle::initTestCase()", hostRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::loadHost()", hostRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::runtimeProperties()", hostRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::unloadHost()", hostRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::cleanupTestCase()", hostRun.StdOut);

            var appRun = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                Args = ["appStartup", "appShutdown"],
                StdErr = Redirect.StdOut
            });

            if (!string.IsNullOrWhiteSpace(appRun.StdOut))
                Console.WriteLine(appRun.StdOut);

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, appRun.ExitCode,
                ExitCodeHelper.ToString(appRun.ExitCode));

            Assert.Contains("PASS   : Test_HostLifecycle::initTestCase()", appRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::appStartup()", appRun.StdOut);
            Assert.Contains("HostLifecycle managed app ready", appRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::appShutdown()", appRun.StdOut);
            Assert.Contains("HostLifecycle managed app stopping", appRun.StdOut);
            Assert.Contains("PASS   : Test_HostLifecycle::cleanupTestCase()", appRun.StdOut);
        }
    }
}
