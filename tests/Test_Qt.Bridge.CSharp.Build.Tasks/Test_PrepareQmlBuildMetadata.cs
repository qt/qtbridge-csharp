// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_PrepareQmlBuildMetadata : TestBase
    {
        [TestMethod]
        public void Execute_GeneratesQrcAndPatchesWorkspaceIni()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(paths.GeneratedSourceDirectory));
            var qmlFile = CreateQmlItem(
                Path.Combine(paths.ProjectSourceDirectory, "Views", "Details.qml"),
                "Views",
                "Details");
            var task = CreateTask(paths);
            task.QmlFiles = [qmlFile];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.IsTrue(task.ProjectSourcesQrcChanged);
            Assert.IsTrue(task.BuildIniChanged);
            Assert.AreEqual(paths.IniPath, task.BuildIniPath);
            Assert.IsNotNull(task.ProjectSourcesQrcPath);
            Assert.IsTrue(File.Exists(task.ProjectSourcesQrcPath));
            var ini = File.ReadAllText(paths.IniPath);
            Assert.Contains(
                $"2\\sourcePath=\"{Normalize(paths.ProjectSourceDirectory)}\"",
                ini);
            Assert.Contains(Normalize(task.ProjectSourcesQrcPath), ini);
        }

        [TestMethod]
        public void Execute_AddsLegacyFallbackPaths()
        {
            var paths = CreatePaths();
            WriteIni(paths, LegacyIni(paths.GeneratedSourceDirectory));
            var importPath = Path.Combine(TempDirectory, "imports");
            var resourcePath = Path.Combine(paths.BuildDirectory, "generated.qrc");
            var task = CreateTask(paths);
            task.ImportPaths = [new TaskItem(importPath)];
            task.ResourceFiles = [new TaskItem(resourcePath)];

            var result = task.Execute();

            Assert.IsTrue(result);
            var content = File.ReadAllText(paths.IniPath);
            var alias = content[content.IndexOf(
                SectionKey(paths.ProjectSourceDirectory),
                StringComparison.Ordinal)..];
            Assert.Contains(Normalize(paths.BuildDirectory), alias);
            Assert.Contains(Normalize(importPath), alias);
            Assert.Contains(Normalize(resourcePath), alias);
        }

        [TestMethod]
        public void Execute_UsesItemSpecWhenSourcePathMetadataIsAbsent()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(paths.GeneratedSourceDirectory));
            var sourcePath = Path.Combine(paths.ProjectSourceDirectory, "Main.qml");
            var qmlFile = new TaskItem(sourcePath);
            qmlFile.SetMetadata("ModulePath", "Application");
            qmlFile.SetMetadata("TypeName", "Main");
            var task = CreateTask(paths);
            task.QmlFiles = [qmlFile];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.Contains(
                "alias=\"Main.qml\"",
                File.ReadAllText(task.ProjectSourcesQrcPath!));
        }

        [TestMethod]
        public void Execute_UsesSourceDirAsModulePath()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(paths.GeneratedSourceDirectory));
            var qmlFile = new TaskItem(
                Path.Combine(paths.ProjectSourceDirectory, "Views", "Details.qml"));
            qmlFile.SetMetadata("SourceDir", @"Views\");
            qmlFile.SetMetadata("TypeName", "Details");
            var task = CreateTask(paths);
            task.QmlFiles = [qmlFile];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.Contains(
                "prefix=\"/qt/qml/Views\"",
                File.ReadAllText(task.ProjectSourcesQrcPath!));
        }

        [TestMethod]
        public void Execute_ReportsMissingQmlMetadataAsBuildError()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(paths.GeneratedSourceDirectory));
            var buildEngine = new TestBuildEngine();
            var task = CreateTask(paths, buildEngine);
            task.QmlFiles = [new TaskItem("Main.qml")];

            var result = task.Execute();

            Assert.IsFalse(result);
            Assert.HasCount(2, buildEngine.Warnings);
            Assert.IsTrue(buildEngine.Warnings.Any(error =>
                error.Message!.Contains("ModulePath", StringComparison.Ordinal)
                && error.Message.Contains("SourceDir", StringComparison.Ordinal)));
            Assert.IsTrue(buildEngine.Warnings.Any(error =>
                error.Message!.Contains("TypeName", StringComparison.Ordinal)));
            Assert.IsNull(task.ProjectSourcesQrcPath);
        }

        [TestMethod]
        public void Execute_ReportsUnreadyIniAsBuildError()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(Path.Combine(TempDirectory, "other")));
            var buildEngine = new TestBuildEngine();
            var task = CreateTask(paths, buildEngine);

            var result = task.Execute();

            Assert.IsFalse(result);
            Assert.HasCount(1, buildEngine.Warnings);
            Assert.Contains(paths.GeneratedSourceDirectory, buildEngine.Warnings[0].Message!);
            Assert.IsFalse(task.BuildIniChanged);
        }

        [TestMethod]
        public void Execute_ReportsMissingIniBeforeWritingQrc()
        {
            var paths = CreatePaths();
            var buildEngine = new TestBuildEngine();
            var task = CreateTask(paths, buildEngine);
            task.QmlFiles =
            [
                CreateQmlItem(
                    Path.Combine(paths.ProjectSourceDirectory, "Main.qml"),
                    "Application",
                    "Main")
            ];

            var result = task.Execute();

            Assert.IsFalse(result);
            Assert.HasCount(1, buildEngine.Warnings);
            Assert.Contains("does not exist", buildEngine.Warnings[0].Message!);
            Assert.IsNull(task.ProjectSourcesQrcPath);
            Assert.IsFalse(Directory.Exists(Path.Combine(paths.BuildDirectory, ".qt")));
        }

        [TestMethod]
        public void Execute_ReportsResourceIdentityCollisionAsBuildError()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(paths.GeneratedSourceDirectory));
            var buildEngine = new TestBuildEngine();
            var task = CreateTask(paths, buildEngine);
            task.QmlFiles =
            [
                CreateQmlItem(
                    Path.Combine(paths.ProjectSourceDirectory, "First", "View.qml"),
                    "Views",
                    "FirstView"),
                CreateQmlItem(
                    Path.Combine(paths.ProjectSourceDirectory, "Second", "View.qml"),
                    "Views",
                    "SecondView")
            ];

            var result = task.Execute();

            Assert.IsTrue(result);
            Assert.HasCount(1, buildEngine.Warnings);
            Assert.Contains("/qt/qml/Views/View.qml", buildEngine.Warnings[0].Message!);
        }

        [TestMethod]
        public void Execute_ReportsUnchangedOutputsOnSecondRun()
        {
            var paths = CreatePaths();
            WriteIni(paths, WorkspaceIni(paths.GeneratedSourceDirectory));
            var qmlFile = CreateQmlItem(
                Path.Combine(paths.ProjectSourceDirectory, "Main.qml"),
                "Application",
                "Main");
            var first = CreateTask(paths);
            first.QmlFiles = [qmlFile];
            Assert.IsTrue(first.Execute());

            var second = CreateTask(paths);
            second.QmlFiles = [qmlFile];
            var result = second.Execute();

            Assert.IsTrue(result);
            Assert.IsFalse(second.ProjectSourcesQrcChanged);
            Assert.IsFalse(second.BuildIniChanged);
        }

        protected override string TempDirectoryName => "qtbridge-prepare-qmlls-tests";

        private TestPaths CreatePaths()
        {
            var buildDirectory = Path.Combine(TempDirectory, "build");
            return new TestPaths(
                buildDirectory,
                Path.Combine(buildDirectory, "qt", "native", "source"),
                Path.Combine(TempDirectory, "Project Sources"),
                Path.Combine(buildDirectory, ".qt", QmllsBuildIniPatcher.FileName));
        }

        private static PrepareQmlBuildMetadata CreateTask(
            TestPaths paths,
            TestBuildEngine? buildEngine = null)
        {
            return new PrepareQmlBuildMetadata
            {
                BuildEngine = buildEngine ?? new TestBuildEngine(),
                BuildDirectory = paths.BuildDirectory,
                GeneratedSourceDirectory = paths.GeneratedSourceDirectory,
                ProjectSourceDirectory = paths.ProjectSourceDirectory
            };
        }

        private static TaskItem CreateQmlItem(
            string sourcePath,
            string modulePath,
            string typeName)
        {
            var item = new TaskItem(sourcePath);
            item.SetMetadata("SourcePath", sourcePath);
            item.SetMetadata("ModulePath", modulePath);
            item.SetMetadata("TypeName", typeName);
            return item;
        }

        private static void WriteIni(TestPaths paths, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(paths.IniPath)!);
            File.WriteAllText(paths.IniPath, content);
        }

        private static string WorkspaceIni(string sourceDirectory)
        {
            return string.Join(
                Environment.NewLine,
                "[workspaces]",
                $"1\\sourcePath=\"{Normalize(sourceDirectory)}\"",
                "1\\importPaths=\"/generated/imports\"",
                "1\\resourceFiles=\"/generated/resources.qrc\"",
                "size=1");
        }

        private static string LegacyIni(string sourceDirectory)
        {
            return string.Join(
                Environment.NewLine,
                SectionKey(sourceDirectory),
                "importPaths=\"/generated/imports\"",
                "resourceFiles=\"/generated/resources.qrc\"");
        }

        private static string SectionKey(string path)
        {
            var normalized = Normalize(path).TrimEnd('/');
            if (normalized is [_, ':', ..])
                normalized = char.ToUpperInvariant(normalized[0]) + normalized[1..];
            return "[" + normalized.Replace("/", "<SLASH>") + "]";
        }

        private sealed record TestPaths(
            string BuildDirectory,
            string GeneratedSourceDirectory,
            string ProjectSourceDirectory,
            string IniPath);

        private sealed class TestBuildEngine : IBuildEngine
        {
            public List<BuildWarningEventArgs> Warnings { get; } = [];

            public bool ContinueOnError => false;

            public int LineNumberOfTaskNode => 0;

            public int ColumnNumberOfTaskNode => 0;

            public string ProjectFileOfTaskNode => "Test.proj";

            public void LogErrorEvent(BuildErrorEventArgs e) { }

            public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);

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
