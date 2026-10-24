// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Extension
{
    [TestClass]
    public sealed class Test_EvaluatedProject
    {
        private static EvaluatedProject Project(
            IEnumerable<string> capabilities,
            params EvaluatedProject.ActiveConfiguration[] configurations)
        {
            return new EvaluatedProject(@"C:\Projects\App\App.csproj", capabilities,
                configurations);
        }

        private static EvaluatedProject.ActiveConfiguration Configuration(params string[] packageIds)
        {
            return new EvaluatedProject.ActiveConfiguration("Debug|AnyCPU",
                [.. packageIds.Select(id => new EvaluatedProject.PackageReference(id, "0.5.0"))]);
        }

        [TestMethod]
        public void Detects_Capability_Case_Insensitively()
        {
            var project = Project(["CSharp", "qtbridgecsharp"]);

            Assert.IsTrue(project.HasCapability);
        }

        [TestMethod]
        public void Has_No_Capability_Before_Restore()
        {
            var project = Project(["CSharp", "Managed"],
                Configuration("QtGroup.Qt.Bridge.CSharp.win-arm64"));

            Assert.IsFalse(project.HasCapability);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.win-arm64", project.PackageId);
        }

        [TestMethod]
        public void Finds_Package_Among_Other_References()
        {
            var project = Project([],
                Configuration("Newtonsoft.Json", "QtGroup.Qt.Bridge.CSharp.linux-x64", "Moq"));

            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.linux-x64", project.PackageId);
        }

        [TestMethod]
        public void Finds_Package_In_Any_Active_Configuration()
        {
            var project = Project([],
                Configuration(),
                Configuration("QtGroup.Qt.Bridge.CSharp.win-x64"));

            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.win-x64", project.PackageId);
        }

        [TestMethod]
        public void Reports_No_Package_Without_Qt_Bridge_Reference()
        {
            var project = Project(["CSharp"],
                Configuration("Newtonsoft.Json", "QtGroup.Something.Else"));

            Assert.IsFalse(project.HasCapability);
            Assert.IsNull(project.PackageId);
        }
    }
}
