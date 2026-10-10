// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.Models.ModelFixtures
{
    [TestClass]
    public class Test_ModelFixtures : ManagedTestBase
    {
        private static TempProject sharedProject;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static async Task BuildTestProject(TestContext _)
        {
            sharedProject = new TempProject();
            var rootPath = Path.Combine("Models", "ModelFixtures");
            var options = CreateQtQuickTestOptions(Path.Combine(rootPath, "main.cpp"));

            try {
                await InitializeAndBuildAsync(sharedProject, options, project => {
                    project.CopyFile("Program.cs", Path.Combine(rootPath, "Program.cs"));
                    CopyQml(project, "TableModel", "tst_tablemodel.qml");
                    CopyQml(project, "TreeModel", "tst_treemodel.qml");
                    CopyQml(project, "CollectionModel", "tst_collectionmodel.qml");
                    CopyQml(project, "TableModelRemoveRanges", "tst_modelremoveranges.qml");
                    CopyQml(project, "CachedProperty", "tst_cachedproperty.qml");
                    CopyQml(project, "ObservableModel", "tst_observablemodel.qml");
                });
            } catch {
                sharedProject.Dispose();
                sharedProject = null;
                throw;
            }
        }

        [ClassCleanup]
        public static void CleanupTestProject()
        {
            sharedProject?.Dispose();
            sharedProject = null;
        }

        [TestMethod]
        public Task TableModel_Read_Insert_Rows_Columns()
            => RunQmlTestAsync("tst_tablemodel.qml");

        [TestMethod]
        public Task MetadataTreeModel_ExpandsInTreeView()
            => RunQmlTestAsync("tst_treemodel.qml");

        [TestMethod]
        public Task MetadataCollectionModels_Expose_Item_And_Property_Roles()
            => RunQmlTestAsync("tst_collectionmodel.qml");

        [TestMethod]
        public Task ModelRemoveRanges()
            => RunQmlTestAsync("tst_modelremoveranges.qml");

        [TestMethod]
        public Task CachedCollectionProperty_Survives_Garbage_Collection()
            => RunQmlTestAsync("tst_cachedproperty.qml");

        [TestMethod]
        public Task ObservableModels_Apply_Changes_As_Deltas()
            => RunQmlTestAsync("tst_observablemodel.qml");

        private static void CopyQml(TempProject project, string fixture, string fileName)
        {
            project.CopyFile(fileName, Path.Combine("Models", fixture, fileName));
        }

        private async Task RunQmlTestAsync(string fileName)
        {
            var project = sharedProject
                ?? throw new InvalidOperationException("The shared model test project was not built.");
            var run = await project.RunAsync(new() {
                Args = ["-input", Path.Combine(project.ExeDir, "Application", fileName)],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            var messages = ParseQtTestMessages(run.StdOut);
            messages.Fail.ForEach(message => TestContext.WriteLine(message));
            messages.Warning.ForEach(message => TestContext.WriteLine(message));

            AssertQTestExitCode(run.ExitCode, run.StdOut);
            Assert.IsEmpty(messages.Fail);
        }
    }
}
