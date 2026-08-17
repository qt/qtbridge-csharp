// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.CollectionsAndValues
{
    [TestClass]
    public class Test_CollectionsAndValues : ManagedTestBase
    {
        [TestMethod]
        public async Task Collections_Fields_And_Value_Marshaling()
        {
            using var temp = new TempProject();

            var options = new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", @"CollectionsAndValues\main.cpp"),
                    (@"source\hpp\QtTestSetupBase.h", @"Shared\QtTestSetupBase.h"),
                    (@"source\hpp\StringBuilder.h", @"Shared\StringBuilder.h"),
                    (@"source\cpp\StringBuilder.cpp", @"Shared\StringBuilder.cpp"),
                ],
                AfterSdkTargets = CMake.InjectQtSourcesTargets(
                    "hpp/QtTestSetupBase.h",
                    "hpp/StringBuilder.h",
                    "cpp/StringBuilder.cpp")
            };

            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.CopyFile("Program.cs", Path.Combine("CollectionsAndValues", "Program.cs"));
            });

            var run = await temp.RunAsync(new() {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                $"""
                {ExitCodeHelper.ToString(run.ExitCode)}
                {run.StdOut}
                {run.StdErr}
                """);

            Assert.Contains("PASS   : Test_CollectionsAndValues::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::arrayOfInts()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::arrayOfStrings()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::arrayOfObjects()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::stringMarshal()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::fieldAccess()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::modelIndexMarshal()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::dateTimeMarshal()", run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::uriMarshal()", run.StdOut);
            Assert.Contains(
                "PASS   : Test_CollectionsAndValues::qcharWidthDiffersFromWcharOnThisPlatform()",
                run.StdOut);
            Assert.Contains(
                OperatingSystem.IsWindows()
                    ? "SKIP   : Test_CollectionsAndValues::utf16TerminatedCopyPreservesTail()"
                    : "PASS   : Test_CollectionsAndValues::utf16TerminatedCopyPreservesTail()",
                run.StdOut);
            Assert.Contains("PASS   : Test_CollectionsAndValues::cleanupTestCase()", run.StdOut);
        }
    }
}
