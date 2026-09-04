// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.Models.CollectionModel
{
    [TestClass]
    public class Test_CollectionModel : ManagedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task MetadataCollectionModels_Expose_Item_And_Property_Roles()
        {
            const string rootPath = "Models/CollectionModel";

            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine(rootPath, "main.cpp"));
            // TODO: Revisit if QtExportAs ever becomes a generator project default rather
            //       than an assembly-level Global Export attribute injected by the build.
            options.AfterSdkProps = """
                <PropertyGroup>
                  <QtExportAs>metadata</QtExportAs>
                </PropertyGroup>
                """;
            await InitializeAndBuildAsync(temp, options, project => {
                project.CopyFile("Program.cs", Path.Combine(rootPath, "Program.cs"));
                project.CopyFile("tst_collectionmodel.qml",
                    Path.Combine(rootPath, "tst_collectionmodel.qml"));
            });

            var run = await temp.RunAsync(new() {
                Args = [
                    "-input", Path.Combine(temp.ExeDir, "Application", "tst_collectionmodel.qml")
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

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));
            Assert.IsEmpty(message.Fail);
        }
    }
}
