// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.ModelRemoveRanges
{
    [TestClass]
    public class Test_ModelRemoveRanges : ManagedTestBase
    {
        [TestMethod]
        public async Task ModelRemoveRanges()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine("QtQuickTest", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs",
                        Path.Combine("Models", "TableModelRemoveRanges", "Program.cs"));
                    project.CopyFile("tst_modelremoveranges.qml", Path
                        .Combine("Models", "TableModelRemoveRanges", "tst_modelremoveranges.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = [
                    "-input", Path.Combine(temp.ExeDir, "Application", "tst_modelremoveranges.qml")
                ],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                $"{ExitCodeHelper.ToString(run.ExitCode)}{Environment.NewLine}{run.StdOut}");

            var pass = "PASS   : Test_QtQuickTest::tst_modelremoveranges::";
            Assert.Contains(pass + "initTestCase()", run.StdOut);
            Assert.Contains(pass + "test_removeRows_reportsInclusiveRange()", run.StdOut);
            Assert.Contains(pass + "test_removeColumns_reportsInclusiveRange()", run.StdOut);
            Assert.Contains(pass + "cleanupTestCase()", run.StdOut);
        }
    }
}
