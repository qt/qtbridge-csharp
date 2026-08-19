// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.Models.TreeModel
{
    [TestClass]
    public class Test_TreeModel : ManagedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task MetadataTreeModel_ExpandsInTreeView()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine(
                "Models", "TreeModel", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", Path.Combine(
                        "Models", "TreeModel", "Program.cs"));
                    project.CopyFile("tst_treemodel.qml", Path.Combine(
                        "Models", "TreeModel", "tst_treemodel.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_treemodel.qml")],
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
