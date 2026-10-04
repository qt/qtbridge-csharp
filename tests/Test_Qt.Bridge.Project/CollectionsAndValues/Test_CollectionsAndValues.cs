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

            AssertQTestExitCode(run.ExitCode,
                $"{run.StdOut}{Environment.NewLine}{run.StdErr}");

            var msgs = ParseQtTestMessages(run.StdOut);
            Assert.Contains(m => m.Contains("initTestCase"), msgs.Pass);
            Assert.Contains(m => m.Contains("arrayOfInts"), msgs.Pass);
            Assert.Contains(m => m.Contains("arrayOfStrings"), msgs.Pass);
            Assert.Contains(m => m.Contains("arrayOfObjects"), msgs.Pass);
            Assert.Contains(m => m.Contains("stringMarshal"), msgs.Pass);
            Assert.Contains(m => m.Contains("fieldAccess"), msgs.Pass);
            Assert.Contains(m => m.Contains("modelIndexMarshal"), msgs.Pass);
            Assert.Contains(m => m.Contains("dateTimeMarshal"), msgs.Pass);
            Assert.Contains(m => m.Contains("uriMarshal"), msgs.Pass);
            Assert.Contains(m => m.Contains("qcharWidthDiffersFromWcharOnThisPlatform"), msgs.Pass);
            if (OperatingSystem.IsWindows())
                Assert.Contains(m => m.Contains("utf16TerminatedCopyPreservesTail"), msgs.Skip);
            else
                Assert.Contains(m => m.Contains("utf16TerminatedCopyPreservesTail"), msgs.Pass);
            Assert.Contains(m => m.Contains("convertToQVariant"), msgs.Pass);
            Assert.DoesNotContain(m => m.Contains("convertToQVariant"), msgs.Fail);
            Assert.Contains(m => m.Contains("cleanupTestCase"), msgs.Pass);
        }
    }
}
