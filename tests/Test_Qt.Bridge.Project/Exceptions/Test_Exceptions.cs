// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.Exceptions
{
    [TestClass]
    public class Test_Exceptions : ManagedTestBase
    {
        [TestMethod]
        public async Task Handle_Exceptions()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine("Exceptions", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", Path.Combine("Exceptions", "Program.cs"));
                    project.CopyFile("tst_exceptions.qml", Path
                        .Combine("Exceptions", "tst_exceptions.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_exceptions.qml")],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            AssertQTestExitCode(run.ExitCode);
            var msgs = ParseQtTestMessages(run.StdOut);
            Assert.IsEmpty(msgs.Fail);
            Assert.Contains(msg => msg.Contains("[[METADATA::Method]]"), msgs.Warning);
            Assert.Contains(msg => msg.Contains("[[METADATA::Property::get]]"), msgs.Warning);
            Assert.Contains(msg => msg.Contains("[[METADATA::Property::set]]"), msgs.Warning);
            Assert.Contains(msg => msg.Contains("[[SOURCE::Method]]"), msgs.Warning);
            Assert.Contains(msg => msg.Contains("[[SOURCE::Property::get]]"), msgs.Warning);
            Assert.Contains(msg => msg.Contains("[[SOURCE::Property::set]]"), msgs.Warning);
        }
    }
}
