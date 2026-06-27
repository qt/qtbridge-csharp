// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_PublishedQmlBuildMetadataValidator : TestBase
    {
        [TestMethod]
        public void Validate_AcceptsWorkspaceAliasAndProjectQrc()
        {
            var paths = CreatePaths();
            var projectQrcPath = ProjectQrcPath(paths.BuildDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(paths.IniPath)!);
            File.WriteAllText(paths.IniPath, string.Join(
                Environment.NewLine,
                "[workspaces]",
                $"1\\sourcePath=\"{Normalize(paths.GeneratedSourceDirectory)}\"",
                "1\\importPaths=\"/generated/imports\"",
                "1\\resourceFiles=\"/generated/resources.qrc\"",
                $"2\\sourcePath=\"{Normalize(paths.ProjectSourceDirectory)}\"",
                "2\\importPaths=\"/generated/imports\"",
                $"2\\resourceFiles=\"/generated/resources.qrc;{Normalize(projectQrcPath)}\"",
                "size=2"));
            File.WriteAllText(projectQrcPath, "<RCC />");

            var error = QmlBuildMetadataValidator.Validate(
                paths.IniPath,
                paths.ProjectSourceDirectory,
                projectQrcPath);

            Assert.IsNull(error);
        }

        [TestMethod]
        public void Validate_RejectsMissingWorkspaceAlias()
        {
            var paths = CreatePaths();
            Directory.CreateDirectory(Path.GetDirectoryName(paths.IniPath)!);
            File.WriteAllText(paths.IniPath, string.Join(
                Environment.NewLine,
                "[workspaces]",
                $"1\\sourcePath=\"{Normalize(paths.GeneratedSourceDirectory)}\"",
                "1\\importPaths=\"/generated/imports\"",
                "1\\resourceFiles=\"/generated/resources.qrc\"",
                "size=1"));

            var error = QmlBuildMetadataValidator.Validate(
                paths.IniPath,
                paths.ProjectSourceDirectory,
                null);

            Assert.IsNotNull(error);
            Assert.Contains("alias is missing", error);
        }

        [TestMethod]
        public void Validate_RejectsMissingProjectQrcReference()
        {
            var paths = CreatePaths();
            var projectQrcPath = ProjectQrcPath(paths.BuildDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(paths.IniPath)!);
            File.WriteAllText(paths.IniPath, string.Join(
                Environment.NewLine,
                "[workspaces]",
                $"1\\sourcePath=\"{Normalize(paths.GeneratedSourceDirectory)}\"",
                "1\\importPaths=\"/generated/imports\"",
                "1\\resourceFiles=\"/generated/resources.qrc\"",
                $"2\\sourcePath=\"{Normalize(paths.ProjectSourceDirectory)}\"",
                "2\\importPaths=\"/generated/imports\"",
                "2\\resourceFiles=\"/generated/resources.qrc\"",
                "size=2"));
            File.WriteAllText(projectQrcPath, "<RCC />");

            var error = QmlBuildMetadataValidator.Validate(
                paths.IniPath,
                paths.ProjectSourceDirectory,
                projectQrcPath);

            Assert.IsNotNull(error);
            Assert.Contains("does not reference", error);
        }

        [TestMethod]
        public void Validate_RejectsMissingProjectQrcFile()
        {
            var paths = CreatePaths();
            var projectQrcPath = ProjectQrcPath(paths.BuildDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(paths.IniPath)!);
            File.WriteAllText(paths.IniPath, string.Join(
                Environment.NewLine,
                SectionKey(paths.ProjectSourceDirectory),
                $"resourceFiles=\"{Normalize(projectQrcPath)}\""));

            var error = QmlBuildMetadataValidator.Validate(
                paths.IniPath,
                paths.ProjectSourceDirectory,
                projectQrcPath);

            Assert.IsNotNull(error);
            Assert.Contains("does not exist", error);
        }

        protected override string TempDirectoryName => "qtbridge-published-qmlls-validator-tests";

        private static string ProjectQrcPath(string buildDirectory) =>
            Path.Combine(buildDirectory, ".qt", ProjectSourcesQrcWriter.FileName);

        private TestPaths CreatePaths()
        {
            var buildDirectory = Path.Combine(TempDirectory, "build");
            return new TestPaths(
                buildDirectory,
                Path.Combine(buildDirectory, "qt", "native", "source"),
                Path.Combine(TempDirectory, "Project Sources"),
                Path.Combine(buildDirectory, ".qt", QmllsBuildIniPatcher.FileName));
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
    }
}
