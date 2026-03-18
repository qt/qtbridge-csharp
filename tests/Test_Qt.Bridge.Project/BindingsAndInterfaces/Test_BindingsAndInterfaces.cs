// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.BindingsAndInterfaces
{
    [TestClass]
    public class Test_BindingsAndInterfaces : ManagedTestBase
    {
        [TestMethod]
        public async Task Bindings_And_Interfaces()
        {
            using var temp = new TempProject();

            var options = new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", @"BindingsAndInterfaces\main.cpp"),
                    (@"source\hpp\QtTestSetupBase.h", @"Shared\QtTestSetupBase.h"),
                    (@"source\hpp\TransformedTextSource.h",
                        @"BindingsAndInterfaces\TransformedTextSource.h"),
                    (@"source\cpp\TransformedTextSource.cpp",
                        @"BindingsAndInterfaces\TransformedTextSource.cpp"),
                ],
                AfterSdkTargets = CMake.InjectQtSourcesTargets(
                    "hpp/QtTestSetupBase.h",
                    "hpp/TransformedTextSource.h",
                    "cpp/TransformedTextSource.cpp")
            };
            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("BindingsAndInterfaces", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            if (!string.IsNullOrWhiteSpace(run.StdOut))
                Console.WriteLine(run.StdOut);

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_BindingsAndInterfaces::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_BindingsAndInterfaces::propertyBinding()", run.StdOut);
            Assert.Contains("PASS   : Test_BindingsAndInterfaces::implementInterface()", run.StdOut);
            Assert.Contains("PASS   : Test_BindingsAndInterfaces::cleanupTestCase()", run.StdOut);
        }
    }
}
