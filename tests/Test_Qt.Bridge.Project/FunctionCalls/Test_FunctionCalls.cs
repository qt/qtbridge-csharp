// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.FunctionCalls
{
    [TestClass]
    public class Test_FunctionCalls : ManagedTestBase
    {
        [TestMethod]
        public async Task DirectFunctionResolution()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine("FunctionCalls", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("FunctionCalls", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_FunctionCalls::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_FunctionCalls::resolveFunction()", run.StdOut);
            Assert.Contains("PASS   : Test_FunctionCalls::callFunction()", run.StdOut);
            Assert.Contains("PASS   : Test_FunctionCalls::callDefaultEntryPoint()", run.StdOut);
            Assert.Contains("PASS   : Test_FunctionCalls::cleanupTestCase()", run.StdOut);
        }
    }
}
