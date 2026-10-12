// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.QMetaObjectBuilder
{
    [TestClass]
    public class Test_QMetaObjectBuilder : ManagedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task MetadataMethodsAndEventsUseQtMetaObjectIndices()
        {
            const string rootPath = "QMetaObjectBuilder";

            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine(rootPath, "main.cpp"));
            options.AfterSdkProps = """
                <PropertyGroup>
                  <QtExportAs>metadata</QtExportAs>
                </PropertyGroup>
                """;
            await InitializeAndBuildAsync(temp, options, project => {
                project.CopyFile("Program.cs", Path.Combine(rootPath, "Program.cs"));
                project.CopyFile("tst_qmetaobjectbuilder.qml",
                    Path.Combine(rootPath, "tst_qmetaobjectbuilder.qml"));
            });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir,
                    "Application", "tst_qmetaobjectbuilder.qml")],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            AssertQTestExitCode(run.ExitCode, run.StdOut);

            var messages = ParseQtTestMessages(run.StdOut);
            messages.Fail.ForEach(msg => TestContext.WriteLine(msg));
            messages.Warning.ForEach(msg => TestContext.WriteLine(msg));

            const string fixture = "Test_QMetaObjectBuilder::tst_qmetaobjectbuilder";
            Assert.Contains($"PASS   : {fixture}::test_metadata_method_dispatch_with_signals()",
                run.StdOut);
            Assert.Contains($"PASS   : {fixture}::test_metadata_event_delivery()", run.StdOut);
            Assert.IsEmpty(messages.Fail);
        }
    }
}
