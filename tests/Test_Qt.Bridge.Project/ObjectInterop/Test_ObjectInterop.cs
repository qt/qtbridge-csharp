// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.ObjectInterop
{
    [TestClass]
    public class Test_ObjectInterop : ManagedTestBase
    {
        [TestMethod]
        public async Task CreateObjects_CallMethods_AndHandleExceptions()
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine("ObjectInterop", "main.cpp"));
            options.BeforeSdkProps += """
                <PropertyGroup>
                  <QtDotNetSafeMethod>enable</QtDotNetSafeMethod>
                </PropertyGroup>
                """;
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("ObjectInterop", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_ObjectInterop::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_ObjectInterop::createObject()", run.StdOut);
            Assert.Contains("PASS   : Test_ObjectInterop::callInstanceMethod()", run.StdOut);
            Assert.Contains("PASS   : Test_ObjectInterop::handleException()", run.StdOut);
            Assert.Contains("PASS   : Test_ObjectInterop::cleanupTestCase()", run.StdOut);
        }
    }
}
