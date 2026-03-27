// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.Models.TableModel
{
    [TestClass]
    public class Test_TableModel : ManagedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task TableModel_Read_Insert_Rows_Columns()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine(
                "Models", "TableModel", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", Path.Combine(
                        "Models", "TableModel", "Program.cs"));
                    project.CopyFile("tst_tablemodel.qml", Path.Combine(
                        "Models", "TableModel", "tst_tablemodel.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_tablemodel.qml")],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            var msgs = ParseQtTestMessages(run.StdOut);
            msgs.Fail.ForEach(msg => TestContext.WriteLine(msg));
            msgs.Warning.ForEach(msg => TestContext.WriteLine(msg));

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));
            Assert.IsEmpty(msgs.Fail);
        }
    }
}
