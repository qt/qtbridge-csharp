// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_QtBridgeResolveResxFileRefs : TestBase
    {
        [TestMethod]
        public void Execute_ResolvesProjectRelativeResxFileRefs()
        {
            var projectDir = Path.Combine(TempDirectory, "Project");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "image.png"), "png");
            File.WriteAllText(Path.Combine(projectDir, "Resources.resx"), ResxWithFile("image.png"));

            var task = CreateTask(projectDir);
            task.ResxFiles = [new TaskItem(Path.Combine(projectDir, "Resources.resx"))];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.HasCount(1, task.ResolvedResources);
            Assert.AreEqual(Normalize(Path.Combine(projectDir, "image.png")),
                Normalize(task.ResolvedResources[0].ItemSpec));
            Assert.AreEqual("assemblies/TestAssembly/image.png",
                task.ResolvedResources[0].GetMetadata("Alias"));
            Assert.AreEqual("Resources.resx::Entry", task.ResolvedResources[0].GetMetadata("Key"));
            Assert.AreEqual("Default", task.ResolvedResources[0].GetMetadata("AccessMode"));
            Assert.HasCount(0, task.ManagedEmbeddedResxFiles);
        }

        [TestMethod]
        public void Execute_AppliesAccessOverridesAndManagedEmbeddingPolicy()
        {
            var projectDir = Path.Combine(TempDirectory, "Project");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "managed.txt"), "managed");
            File.WriteAllText(Path.Combine(projectDir, "Resources.resx"), ResxWithFile("managed.txt"));
            var task = CreateTask(projectDir);
            task.ResxFiles = [new TaskItem(Path.Combine(projectDir, "Resources.resx"))];
            var access = new TaskItem("Resources.resx::Entry");
            access.SetMetadata("Mode", "ManagedOnly");
            access.SetMetadata("Reason", "Keep managed");
            task.ResourceAccessOverrides = [access];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.AreEqual("ManagedOnly", task.ResolvedResources[0].GetMetadata("AccessMode"));
            Assert.AreEqual("Keep managed", task.ResolvedResources[0].GetMetadata("Reason"));
            Assert.HasCount(1, task.ManagedEmbeddedResxFiles);
            Assert.AreEqual(Normalize(Path.Combine(projectDir, "Resources.resx")),
                Normalize(task.ManagedEmbeddedResxFiles[0].ItemSpec));
        }

        [TestMethod]
        public void Execute_ReportsUnmatchedAccessOverrideAsError()
        {
            var projectDir = Path.Combine(TempDirectory, "Project");
            Directory.CreateDirectory(projectDir);
            File.WriteAllText(Path.Combine(projectDir, "image.png"), "png");
            File.WriteAllText(Path.Combine(projectDir, "Resources.resx"), ResxWithFile("image.png"));
            var buildEngine = new TestBuildEngine();
            var task = CreateTask(projectDir, buildEngine);
            task.ResxFiles = [new TaskItem(Path.Combine(projectDir, "Resources.resx"))];
            task.ResourceAccessOverrides = [new TaskItem("Resources.resx::Missing")];

            var result = task.Execute();

            Assert.IsFalse(result);
            Assert.HasCount(1, buildEngine.Errors);
            Assert.Contains("does not match any resolved resource", buildEngine.Errors[0].Message!);
        }

        [TestMethod]
        public void Execute_ResolvesWindowsStyleResxFileRefsOnNonWindowsHosts()
        {
            var projectDir = Path.Combine(TempDirectory, "Project");
            Directory.CreateDirectory(Path.Combine(projectDir, "images"));
            File.WriteAllText(Path.Combine(projectDir, "images", "cover.png"), "png");
            File.WriteAllText(Path.Combine(projectDir, "Resources.resx"),
                ResxWithFile(@"images\cover.png", "Cover"));

            var task = CreateTask(projectDir);
            task.ResxFiles = [new TaskItem(Path.Combine(projectDir, "Resources.resx"))];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.HasCount(1, task.ResolvedResources);
            Assert.AreEqual("Resources.resx::Cover", task.ResolvedResources[0].GetMetadata("Key"));
        }

        protected override string TempDirectoryName => "qtbridge-resx-task-tests";

        private static QtBridgeResolveResxFileRefs CreateTask(
            string projectDir,
            TestBuildEngine? buildEngine = null)
        {
            return new QtBridgeResolveResxFileRefs
            {
                BuildEngine = buildEngine ?? new TestBuildEngine(),
                ProjectDir = projectDir,
                AssemblyResourceId = "TestAssembly"
            };
        }

        private static string ResxWithFile(string fileName, string key = "Entry") =>
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <root>
               <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
               <resheader name="version"><value>2.0</value></resheader>
               <data name="{key}" type="System.Resources.ResXFileRef, System.Windows.Forms">
                 <value>{fileName};System.String, mscorlib;utf-8</value>
               </data>
             </root>
             """;

        private sealed class TestBuildEngine : IBuildEngine
        {
            public List<BuildErrorEventArgs> Errors { get; } = [];

            public bool ContinueOnError => false;

            public int LineNumberOfTaskNode => 0;

            public int ColumnNumberOfTaskNode => 0;

            public string ProjectFileOfTaskNode => "Test.proj";

            public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);

            public void LogWarningEvent(BuildWarningEventArgs e) { }

            public void LogMessageEvent(BuildMessageEventArgs e) { }

            public void LogCustomEvent(CustomBuildEventArgs e) { }

            public bool BuildProjectFile(
                string projectFileName,
                string[] targetNames,
                IDictionary globalProperties,
                IDictionary targetOutputs) => true;
        }
    }
}
