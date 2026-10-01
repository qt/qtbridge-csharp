// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    [TestClass]
    public class Test_QtBridgeQtDirResolver
    {
        [TestMethod]
        public void Prefers_Project_QtDir_Over_Environment()
        {
            var project = CreateProject(("QtDir", @"C:\Qt\6.10.0\msvc2022_arm64"));

            Assert.AreEqual(@"C:\Qt\6.10.0\msvc2022_arm64",
                QtBridgeQtDirResolver.Resolve(project, Env(("QTDIR", @"C:\Qt\Env"))));
        }

        [TestMethod]
        public void Falls_Back_To_QTDIR_Environment_Variable()
        {
            Assert.AreEqual(@"C:\Qt\Env",
                QtBridgeQtDirResolver.Resolve(CreateProject(), Env(("QTDIR", @" C:\Qt\Env "))));
        }

        [TestMethod]
        public void Ignores_QtInstallRoot()
        {
            var project = CreateProject(("QtInstallRoot", @"C:\Qt\Root"));

            Assert.IsNull(QtBridgeQtDirResolver.Resolve(project,
                Env(("QtInstallRoot", @"C:\Qt\EnvRoot"))));
        }

        [TestMethod]
        public void Expands_Property_And_Environment_References()
        {
            var project = CreateProject(("QtDir", @"$(QtRoot)\$(QtArch)"),
                ("QtRoot", @"C:\Qt\6.10.0"));

            Assert.AreEqual(@"C:\Qt\6.10.0\msvc2022_arm64",
                QtBridgeQtDirResolver.Resolve(project, Env(("QtArch", "msvc2022_arm64"))));
        }

        [TestMethod]
        public void Skips_Values_With_Unresolved_References()
        {
            var project = CreateProject(("QtDir", @"$(UnknownRoot)\qt"));

            Assert.AreEqual(@"C:\Qt\Env",
                QtBridgeQtDirResolver.Resolve(project, Env(("QTDIR", @"C:\Qt\Env"))));
            Assert.IsNull(QtBridgeQtDirResolver.Resolve(project, Env()));
        }

        [TestMethod]
        public void Returns_Null_Without_Any_Configuration()
        {
            Assert.IsNull(QtBridgeQtDirResolver.Resolve(CreateProject(), Env()));
        }

        private static QtBridgeProjectMetadata CreateProject(
            params (string Name, string Value)[] properties)
        {
            return new QtBridgeProjectMetadata(
                projectFilePath: @"C:\App\App.csproj",
                type: QtBridgeProjectType.QtBridgeCSharp,
                isQtBridgeProject: true,
                matchedPackageId: null,
                importsQtBridgeProps: false,
                importsQtBridgeTargets: false,
                properties: properties.ToDictionary(
                    p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase));
        }

        private static Func<string, string?> Env(params (string Name, string Value)[] variables)
        {
            var values = variables.ToDictionary(
                v => v.Name, v => v.Value, StringComparer.OrdinalIgnoreCase);
            return name => values.TryGetValue(name, out var value) ? value : null;
        }
    }
}
