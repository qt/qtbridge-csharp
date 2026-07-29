// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_QmllsBuildIniPatcher : TestBase
    {
        [TestMethod]
        public void Patch_AddsWorkspaceAliasFromGeneratedWorkspace()
        {
            var paths = CreatePaths();
            var generatedQrc = Normalize(Path.Combine(paths.BuildDirectory, "generated.qrc"));
            var projectQrc = Normalize(Path.Combine(
                paths.BuildDirectory,
                ".qt",
                ProjectSourcesQrcWriter.FileName));
            WriteIni(paths.IniPath, WorkspaceIni(
                paths.GeneratedSourceDirectory,
                importPaths: $"/qt/qml{Path.PathSeparator}/more/qml",
                resourceFiles: generatedQrc));

            var result = Patch(paths, projectSourcesQrcPath: projectQrc);

            Assert.IsTrue(result.IsReady);
            Assert.IsTrue(result.Changed);
            Assert.AreEqual(QmllsBuildIniPatcher.IniFormat.Workspaces, result.Format);
            var content = File.ReadAllText(paths.IniPath);
            Assert.Contains($"2\\sourcePath=\"{Normalize(paths.ProjectDirectory)}\"", content);
            Assert.Contains(
                $"2\\importPaths=\"/qt/qml{Path.PathSeparator}/more/qml\"", content);
            Assert.Contains(
                $"2\\resourceFiles=\"{generatedQrc}{Path.PathSeparator}{projectQrc}\"",
                content);
            Assert.Contains("size=2", content);

            var directoryName = Path.GetDirectoryName(paths.IniPath);
            Assert.IsNotNull(directoryName);
            Assert.AreEqual(0, Directory.GetFiles(directoryName, "*.tmp").Length);
        }

        [TestMethod]
        public void Patch_UpdatesWorkspaceAliasWithoutDuplicatingResourcePath()
        {
            var paths = CreatePaths();
            var projectQrc = Normalize(Path.Combine(
                paths.BuildDirectory,
                ".qt",
                ProjectSourcesQrcWriter.FileName));
            WriteIni(paths.IniPath, WorkspaceIni(
                paths.GeneratedSourceDirectory,
                importPaths: "/current/imports",
                resourceFiles: projectQrc,
                aliasDirectory: paths.ProjectDirectory,
                aliasImportPaths: "/stale/imports"));

            var first = Patch(paths, projectSourcesQrcPath: projectQrc);
            var second = Patch(paths, projectSourcesQrcPath: projectQrc);

            Assert.IsTrue(first.Changed);
            Assert.IsFalse(second.Changed);
            var content = File.ReadAllText(paths.IniPath);
            Assert.AreEqual(1, File.ReadAllLines(paths.IniPath).Count(line =>
                line == $"2\\resourceFiles=\"{projectQrc}\""));
            Assert.Contains("2\\importPaths=\"/current/imports\"", content);
        }

        [TestMethod]
        public void Patch_PreservesSectionsOutsideWorkspaces()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath,
                "[general]" + Environment.NewLine
                + "option=true" + Environment.NewLine
                + WorkspaceIni(paths.GeneratedSourceDirectory) + Environment.NewLine
                + "[plugins]" + Environment.NewLine
                + "enabled=true");

            var result = Patch(paths);

            Assert.IsTrue(result.IsReady);
            var content = File.ReadAllText(paths.IniPath);
            Assert.Contains("[general]" + Environment.NewLine + "option=true", content);
            Assert.Contains("[plugins]" + Environment.NewLine + "enabled=true", content);
        }

        [TestMethod]
        public void Patch_AcceptsSlashSeparatedWorkspaceEntries()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath,
                string.Join(
                    Environment.NewLine,
                    "[workspaces]",
                    $"1/sourcePath=\"{Normalize(paths.GeneratedSourceDirectory)}\"",
                    "1/importPaths=\"/qt/qml\"",
                    "1/resourceFiles=\"\"",
                    "size=1"));

            var result = Patch(paths);

            Assert.IsTrue(result.IsReady);
            Assert.IsTrue(result.Changed);
            var content = File.ReadAllText(paths.IniPath);
            Assert.Contains($"2\\sourcePath=\"{Normalize(paths.ProjectDirectory)}\"", content);
        }

        [TestMethod]
        public void Patch_ReturnsNotReadyWhenGeneratedWorkspaceIsMissing()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath, WorkspaceIni(Path.Combine(TempDirectory, "other")));
            var original = File.ReadAllText(paths.IniPath);

            var result = Patch(paths);

            Assert.IsFalse(result.IsReady);
            Assert.IsFalse(result.Changed);
            Assert.AreEqual(original, File.ReadAllText(paths.IniPath));
        }

        [TestMethod]
        public void Patch_AddsLegacyAliasWithFallbackPaths()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath, LegacyIni(
                paths.GeneratedSourceDirectory,
                "importPaths=\"/generated/imports\"",
                "resourceFiles=\"/generated/resources.qrc\""));
            var importPath = Normalize(Path.Combine(paths.BuildDirectory, "imports"));
            var resourcePath = Normalize(Path.Combine(paths.BuildDirectory, "fallback.qrc"));

            var result = Patch(paths, [importPath], [resourcePath]);

            Assert.IsTrue(result.IsReady);
            Assert.IsTrue(result.Changed);
            Assert.AreEqual(QmllsBuildIniPatcher.IniFormat.LegacySections, result.Format);
            var alias = File.ReadAllText(paths.IniPath)[File.ReadAllText(paths.IniPath)
                .IndexOf(SectionKey(paths.ProjectDirectory), StringComparison.Ordinal)..];
            Assert.Contains(
                $"importPaths=\"/generated/imports{Path.PathSeparator}{importPath}\"", alias);
            Assert.Contains(
                $"resourceFiles=\"/generated/resources.qrc{Path.PathSeparator}{resourcePath}\"",
                alias);
        }

        [TestMethod]
        public void Patch_UpdatesLegacyAliasIdempotently()
        {
            var paths = CreatePaths();
            var importPath = Normalize(Path.Combine(paths.BuildDirectory, "imports"));
            WriteIni(paths.IniPath,
                LegacyIni(paths.GeneratedSourceDirectory, "importPaths=\"\"")
                + Environment.NewLine
                + LegacyIni(paths.ProjectDirectory, "importPaths=\"/stale\""));

            var first = Patch(paths, [importPath]);
            var second = Patch(paths, [importPath]);

            Assert.IsTrue(first.Changed);
            Assert.IsFalse(second.Changed);
            var content = File.ReadAllText(paths.IniPath);
            Assert.AreEqual(1, Count(content, SectionKey(paths.ProjectDirectory)));
            Assert.AreEqual(1, Count(content, importPath));
        }

        [TestMethod]
        public void Patch_UpdatesLegacyAliasImportPathsWhenStale()
        {
            var paths = CreatePaths();
            var importPath = Normalize(Path.Combine(paths.BuildDirectory, "imports"));
            WriteIni(paths.IniPath,
                LegacyIni(paths.GeneratedSourceDirectory, "importPaths=\"\"")
                + Environment.NewLine
                + LegacyIni(paths.ProjectDirectory, "importPaths=\"/stale\""));

            Patch(paths, [importPath]);

            var content = File.ReadAllText(paths.IniPath);
            var aliasKeyPos = content.IndexOf(SectionKey(paths.ProjectDirectory),
                StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, aliasKeyPos);
            Assert.Contains(importPath, content[aliasKeyPos..]);
        }

        [TestMethod]
        public void Patch_ReturnsNotReadyForEmptyLegacyGeneratedSection()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath, SectionKey(paths.GeneratedSourceDirectory));

            var result = Patch(paths);

            Assert.IsFalse(result.IsReady);
            Assert.IsFalse(result.Changed);
        }

        [TestMethod]
        public void Patch_AcceptsWindowsSeparatorsForWindowsWorkspacePaths()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath, WorkspaceIni(@"C:\work\generated"));

            var result = QmllsBuildIniPatcher.Patch(
                paths.IniPath,
                "c:/WORK/generated",
                @"C:\work\project",
                [],
                [],
                @"C:\work\build\.qt\project.qrc");

            Assert.IsTrue(result.IsReady);
            var content = File.ReadAllText(paths.IniPath);
            Assert.Contains("2\\sourcePath=\"C:/work/project\"", content);
            Assert.Contains("C:/work/build/.qt/project.qrc", content);
        }

        [TestMethod]
        public void Patch_PreservesCaseDistinctUnixWorkspacePaths()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath, WorkspaceIni("/work/generated"));
            var original = File.ReadAllText(paths.IniPath);

            var result = QmllsBuildIniPatcher.Patch(
                paths.IniPath,
                "/work/Generated",
                "/work/project",
                [],
                [],
                null);

            Assert.IsFalse(result.IsReady);
            Assert.AreEqual(original, File.ReadAllText(paths.IniPath));
        }

        [TestMethod]
        public void Patch_DoesNotRewriteUnchangedFile()
        {
            var paths = CreatePaths();
            WriteIni(paths.IniPath, WorkspaceIni(
                paths.GeneratedSourceDirectory,
                aliasDirectory: paths.ProjectDirectory));
            var originalWriteTime = DateTime.UtcNow.AddMinutes(-5);
            File.SetLastWriteTimeUtc(paths.IniPath, originalWriteTime);

            var result = Patch(paths);

            Assert.IsTrue(result.IsReady);
            Assert.IsFalse(result.Changed);
            Assert.AreEqual(originalWriteTime, File.GetLastWriteTimeUtc(paths.IniPath));
        }

        [TestMethod]
        public void Patch_ReturnsNotReadyWhenIniFileIsMissing()
        {
            var paths = CreatePaths();

            var result = Patch(paths);

            Assert.IsFalse(result.IsReady);
            Assert.IsFalse(result.Changed);
            Assert.IsFalse(File.Exists(paths.IniPath));
        }

        [TestMethod]
        public void Patch_SkipsFileWhenSourceAndProjectPathsMatch()
        {
            var paths = CreatePaths();

            var result = QmllsBuildIniPatcher.Patch(
                paths.IniPath,
                paths.ProjectDirectory,
                paths.ProjectDirectory,
                [],
                [],
                null);

            Assert.IsTrue(result.IsReady);
            Assert.IsFalse(result.Changed);
            Assert.IsFalse(File.Exists(paths.IniPath));
        }

        protected override string TempDirectoryName => "qtbridge-build-ini-patcher-tests";

        private TestPaths CreatePaths()
        {
            var buildDirectory = Path.Combine(TempDirectory, "build");
            // The use of ø here is intentional, it verifies that non-ASCII paths survive QRC
            // generation INI patching, and MSBuild task handling.
            var projectDirectory = Path.Combine(TempDirectory, "Prøject Sources");
            var generatedSourceDirectory =
                Path.Combine(buildDirectory, "qt", "native", "source");
            var iniPath = Path.Combine(buildDirectory, ".qt", QmllsBuildIniPatcher.FileName);
            return new TestPaths(
                buildDirectory,
                generatedSourceDirectory,
                projectDirectory,
                iniPath);
        }

        private static QmllsBuildIniPatcher.PatchResult Patch(
            TestPaths paths,
            IReadOnlyCollection<string>? importPaths = null,
            IReadOnlyCollection<string>? resourceFiles = null,
            string? projectSourcesQrcPath = null)
        {
            return QmllsBuildIniPatcher.Patch(
                paths.IniPath,
                paths.GeneratedSourceDirectory,
                paths.ProjectDirectory,
                importPaths ?? [],
                resourceFiles ?? [],
                projectSourcesQrcPath);
        }

        private static void WriteIni(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        private static string WorkspaceIni(
            string sourceDirectory,
            string importPaths = "",
            string resourceFiles = "",
            string? aliasDirectory = null,
            string aliasImportPaths = "")
        {
            var lines = new List<string>
            {
                "[workspaces]",
                $"1\\sourcePath=\"{Normalize(sourceDirectory)}\"",
                $"1\\importPaths=\"{importPaths}\"",
                $"1\\resourceFiles=\"{resourceFiles}\""
            };
            if (aliasDirectory != null) {
                lines.Add($"2\\sourcePath=\"{Normalize(aliasDirectory)}\"");
                lines.Add($"2\\importPaths=\"{aliasImportPaths}\"");
                lines.Add("2\\resourceFiles=\"\"");
                lines.Add("size=2");
            } else {
                lines.Add("size=1");
            }
            return string.Join(Environment.NewLine, lines);
        }

        private static string LegacyIni(string sourceDirectory, params string[] values)
        {
            return string.Join(
                Environment.NewLine,
                new[] { SectionKey(sourceDirectory) }.Concat(values));
        }

        private static string SectionKey(string path)
        {
            var normalized = Normalize(path).TrimEnd('/');
            if (normalized is [_, ':', ..])
                normalized = char.ToUpperInvariant(normalized[0]) + normalized[1..];
            return "[" + normalized.Replace("/", "<SLASH>") + "]";
        }

        private static int Count(string value, string part) =>
            value.Split([part], StringSplitOptions.None).Length - 1;

        private sealed record TestPaths(
            string BuildDirectory,
            string GeneratedSourceDirectory,
            string ProjectDirectory,
            string IniPath);
    }
}
