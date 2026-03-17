// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.WrapperTypes
{
    [TestClass]
    public class Test_WrapperTypes : ManagedTestBase
    {
        [TestMethod]
        public async Task WrapperClasses_For_StringBuilder_And_Uri()
        {
            using var temp = new TempProject();

            var options = new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", @"WrapperTypes\main.cpp"),
                    (@"source\hpp\QtTestSetupBase.h", @"Shared\QtTestSetupBase.h"),
                    (@"source\hpp\stringbuilder.h", @"WrapperTypes\stringbuilder.h"),
                    (@"source\cpp\stringbuilder.cpp", @"WrapperTypes\stringbuilder.cpp"),
                    (@"source\hpp\uri.h", @"WrapperTypes\uri.h"),
                    (@"source\cpp\uri.cpp", @"WrapperTypes\uri.cpp"),
                ],
                AfterSdkTargets = CMake.InjectQtSourcesTargets(
                    "hpp/QtTestSetupBase.h",
                    "hpp/stringbuilder.h",
                    "cpp/stringbuilder.cpp",
                    "hpp/uri.h",
                    "cpp/uri.cpp")
            };

            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("WrapperTypes", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            Assert.Contains("PASS   : Test_WrapperTypes::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_WrapperTypes::useWrapperClassForStringBuilder()",
                run.StdOut);
            Assert.Contains("PASS   : Test_WrapperTypes::useWrapperClassForUri()", run.StdOut);
            Assert.Contains("PASS   : Test_WrapperTypes::handleException()", run.StdOut);
            Assert.Contains("PASS   : Test_WrapperTypes::cleanupTestCase()", run.StdOut);
        }
    }
}
