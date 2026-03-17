// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.DelegatesAndEvents
{
    [TestClass]
    public class Test_DelegatesAndEvents : ManagedTestBase
    {
        [TestMethod]
        public async Task Delegates_Events_And_SignalConverters()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine("DelegatesAndEvents", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("DelegatesAndEvents", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_DelegatesAndEvents::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_DelegatesAndEvents::delegates()", run.StdOut);
            Assert.Contains("PASS   : Test_DelegatesAndEvents::emitSignalFromEvent()", run.StdOut);
            Assert.Contains("PASS   : Test_DelegatesAndEvents::signalConverters()", run.StdOut);
            Assert.Contains("PASS   : Test_DelegatesAndEvents::legacySignalConverters()",
                run.StdOut);
            Assert.Contains("PASS   : Test_DelegatesAndEvents::cleanupTestCase()", run.StdOut);
        }
    }
}
