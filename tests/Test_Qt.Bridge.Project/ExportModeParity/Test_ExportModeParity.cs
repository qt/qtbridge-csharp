// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.ExportModeParity
{
    /// <summary>
    /// Runs the same QML test against the same C# types, exported as source code
    /// and as type metadata: QML code must work unchanged with either export mode.
    /// </summary>
    [TestClass]
    public class Test_ExportModeParity : ManagedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow("source")]
        [DataRow("metadata")]
        public async Task QmlApi_IsTheSame_ForBothExportModes(string exportAs)
        {
            const string rootPath = "ExportModeParity";

            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine(rootPath, "main.cpp"));
            options.AfterSdkProps = $"""
                <PropertyGroup>
                  <QtExportAs>{exportAs}</QtExportAs>
                </PropertyGroup>
                """;
            await InitializeAndBuildAsync(temp, options, project => {
                project.CopyFile("Program.cs", Path.Combine(rootPath, "Program.cs"));
                project.CopyFile("tst_exportmodeparity.qml",
                    Path.Combine(rootPath, "tst_exportmodeparity.qml"));
            });

            var run = await temp.RunAsync(new() {
                Args = [
                    "-input", Path.Combine(temp.ExeDir, "Application", "tst_exportmodeparity.qml")
                ],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            var message = ParseQtTestMessages(run.StdOut);
            message.Fail.ForEach(msg => TestContext.WriteLine(msg));
            message.Warning.ForEach(msg => TestContext.WriteLine(msg));

            AssertQTestExitCode(run.ExitCode, run.StdOut);
            Assert.IsEmpty(message.Fail);
            foreach (var function in new[] { "properties", "methods", "signals", "enum" }) {
                Assert.Contains(
                    $"PASS   : Test_ExportModeParity::ExportModeParity::test_{function}()",
                    run.StdOut);
            }
        }
    }
}
