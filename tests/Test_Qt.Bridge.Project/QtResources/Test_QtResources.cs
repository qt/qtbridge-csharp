// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.QtResources
{
    [TestClass]
    public class Test_QtResources : ManagedTestBase
    {
        [TestMethod]
        public async Task ResourcePipeline()
        {
            using var temp = new TempProject();

            var options = new CreationOptions
            {
                Filename = "QtResources",
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", Path.Combine("QtResources", "main.cpp")),
                    (@"source\hpp\QtTestSetupBase.h", Path.Combine("Shared", "QtTestSetupBase.h")),
                ],
                AfterSdkTargets =
                    """
                      <ItemGroup>
                        <QtResource Include="sample.txt" />
                      </ItemGroup>
                    """
                    + CMake.InjectQtSourcesTargets("hpp/QtTestSetupBase.h")
            };

            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.AddFile("sample.txt", "Hello from Qt Resources!\n");
                project.CopyFile("Program.cs", Path.Combine("QtResources", "Program.cs"));
            });

            var run = await temp.RunAsync(new RunOptions {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));
            Assert.Contains("PASS   : Test_QtResources::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::resourceExists()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::resourceContent()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::cleanupTestCase()", run.StdOut);
        }
    }
}
