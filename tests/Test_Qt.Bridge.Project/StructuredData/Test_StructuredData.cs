// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.StructuredData
{
    [TestClass]
    public class Test_StructuredData : ManagedTestBase
    {
        [TestMethod]
        public async Task StructuredData_QmlRoundtrip_Works()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine("StructuredData", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", Path.Combine("StructuredData", "Program.cs"));
                    project.CopyFile("tst_structureddata.qml",
                        Path.Combine("StructuredData", "tst_structureddata.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_structureddata.qml")],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr  = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            const string passPrefix = "PASS   : Test_StructuredData::tst_structureddata::";
            Assert.Contains(passPrefix + "test_person_roundtrip()", run.StdOut);
            Assert.Contains(passPrefix + "test_team_roundtrip()", run.StdOut);
        }
    }
}
