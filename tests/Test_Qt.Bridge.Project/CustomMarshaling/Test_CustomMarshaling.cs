// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.CustomMarshaling
{
    [TestClass]
    public class Test_CustomMarshaling : ManagedTestBase
    {
        [TestMethod]
        public async Task CustomMarshaling_Functions()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine("CustomMarshaling", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("CustomMarshaling", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_CustomMarshaling::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_CustomMarshaling::callFunctionWithCustomMarshaling()",
                run.StdOut);
            Assert.Contains("PASS   : Test_CustomMarshaling::callWithComplexArg()", run.StdOut);
            Assert.Contains("PASS   : Test_CustomMarshaling::cleanupTestCase()", run.StdOut);
        }
    }
}
