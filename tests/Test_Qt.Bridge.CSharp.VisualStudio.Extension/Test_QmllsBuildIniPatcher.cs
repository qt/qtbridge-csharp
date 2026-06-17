// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer;

using CoreQmlMetadata = Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata.QmlMetadata;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Extension
{
    [TestClass]
    public sealed class Test_QmllsBuildIniPatcher
    {
        private string? tempDirectory;

        [TestCleanup]
        public void Cleanup()
        {
            if (tempDirectory != null && Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }

        // --- V2 (workspaces) format ---

        [TestMethod]
        public void V2_AddsAliasWorkspace_WhenGeneratedWorkspaceExists()
        {
            var (sourceDir, buildDir, projectDir, projectFile) = SetupPaths();
            WriteIniFile(buildDir, V2Ini(sourceDir));

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsTrue(result);
            var lines = File.ReadAllLines(IniPath(buildDir));
            Assert.IsTrue(lines.Any(l => l.Contains(Normalize(projectDir))),
                "Expected an alias workspace entry for the project source dir.");
        }

        [TestMethod]
        public void V2_DoesNotDuplicateAlias_WhenAliasAlreadyPresent()
        {
            var (sourceDir, buildDir, projectDir, projectFile) = SetupPaths();
            WriteIniFile(buildDir, V2Ini(sourceDir, aliasDir: projectDir));

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsTrue(result);
            var sourcePathCount = File.ReadAllLines(IniPath(buildDir))
                .Count(l => l.Contains(Normalize(projectDir)));
            Assert.AreEqual(1, sourcePathCount, "Alias workspace must not be duplicated.");
        }

        [TestMethod]
        public void V2_ReturnsFalse_WhenGeneratedWorkspaceMissing()
        {
            var (sourceDir, buildDir, _, projectFile) = SetupPaths();
            var otherDir = Path.Combine(TempDir, "other");
            WriteIniFile(buildDir, V2Ini(otherDir)); // ini contains a different sourcePath

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void V2_SyncsImportPaths_WhenAliasHasStaleValues()
        {
            var (sourceDir, buildDir, projectDir, projectFile) = SetupPaths();
            var importPath = Normalize(Path.Combine(TempDir, "qt", "qml"));
            WriteIniFile(buildDir, V2Ini(sourceDir, aliasDir: projectDir,
                generatedImportPaths: importPath));

            CreatePatcher().TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsTrue(
                File.ReadAllLines(IniPath(buildDir)).Any(l =>
                    l.TrimStart().StartsWith("2\\importPaths", StringComparison.OrdinalIgnoreCase)
                    && l.Contains(importPath)),
                "Alias workspace importPaths should be synced from the generated workspace.");
        }

        // --- V1 (section-based) format ---

        [TestMethod]
        public void V1_AddsAliasSection_WhenGeneratedSectionExists()
        {
            var (sourceDir, buildDir, projectDir, projectFile) = SetupPaths();
            WriteIniFile(buildDir, V1Ini(sourceDir));

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsTrue(result);
            Assert.Contains(SectionKey(projectDir), File.ReadAllText(IniPath(buildDir)));
        }

        [TestMethod]
        public void V1_DoesNotDuplicateAlias_WhenAliasSectionAlreadyPresent()
        {
            var (sourceDir, buildDir, projectDir, projectFile) = SetupPaths();
            WriteIniFile(buildDir, V1Ini(sourceDir) + Environment.NewLine + V1Ini(projectDir));

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsTrue(result);
            var keyCount = File.ReadAllText(IniPath(buildDir))
                .Split([SectionKey(projectDir)], StringSplitOptions.None).Length - 1;
            Assert.AreEqual(1, keyCount, "Alias section must not be duplicated.");
        }

        [TestMethod]
        public void V1_ReturnsFalse_WhenGeneratedSectionMissing()
        {
            var (sourceDir, buildDir, _, projectFile) = SetupPaths();
            var otherDir = Path.Combine(TempDir, "other");
            WriteIniFile(buildDir, V1Ini(otherDir)); // ini contains a different section key

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void V1_SyncsImportPaths_WhenAliasHasStaleValues()
        {
            var (sourceDir, buildDir, projectDir, projectFile) = SetupPaths();
            WriteIniFile(buildDir, V1Ini(sourceDir) + Environment.NewLine + V1Ini(projectDir));

            CreatePatcher().TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            var content = File.ReadAllText(IniPath(buildDir));
            var aliasKeyPos = content.IndexOf(SectionKey(projectDir), StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(aliasKeyPos >= 0);
            Assert.Contains(Normalize(buildDir), content.Substring(aliasKeyPos));
        }

        // --- TryPatch-level ---

        [TestMethod]
        public void ReturnsFalse_WhenIniFileMissing()
        {
            var (sourceDir, buildDir, _, projectFile) = SetupPaths();
            // Build dir exists but no .qt/.qmlls.build.ini

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsFalse(result);
            Assert.IsFalse(File.Exists(IniPath(buildDir)));
        }

        [TestMethod]
        public void ReturnsTrue_WhenSourceDirMatchesProjectDir_WithoutTouchingFiles()
        {
            var (sourceDir, buildDir, _, _) = SetupPaths();
            // Project file sits directly in sourceDir, so generated key == alias key
            var projectFile = Path.Combine(sourceDir, "MyApp.csproj");

            var result = CreatePatcher()
                .TryPatch(CreateMetadata(sourceDir, (string[]) [buildDir]), projectFile);

            Assert.IsTrue(result);
            Assert.IsFalse(File.Exists(IniPath(buildDir)), "Ini file must not be created.");
        }

        // --- Helpers ---

        private string TempDir => tempDirectory ??= Path.Combine(
            Path.GetTempPath(),
            "qtbridge-ini-patcher-tests",
            Guid.NewGuid().ToString("N"));

        private (string, string, string, string) SetupPaths()
        {
            var sourceDir = Path.Combine(TempDir, "build", "qt", "native", "source");
            var buildDir = Path.Combine(TempDir, "build");
            var projectDir = Path.Combine(TempDir, "project");
            Directory.CreateDirectory(projectDir);
            return (sourceDir, buildDir, projectDir, Path.Combine(projectDir, "MyApp.csproj"));
        }

        private static string IniPath(string buildDir) =>
            Path.Combine(buildDir, ".qt", ".qmlls.build.ini");

        private static void WriteIniFile(string buildDir, string content)
        {
            var qtDir = Path.Combine(buildDir, ".qt");
            Directory.CreateDirectory(qtDir);
            File.WriteAllText(IniPath(buildDir), content);
        }

        private static QmllsBuildIniPatcher CreatePatcher() => new(new TestLog());

        private static CoreQmlMetadata CreateMetadata(
            string sourceDir,
            IReadOnlyList<string> buildDirs)
        {
            return new CoreQmlMetadata(version: 1,
                projectFile: "test.csproj",
                configuration: "Debug",
                targetFramework: null,
                qml: new CoreQmlMetadata.QmlSection(
                    sourceDir: sourceDir,
                    projectSourceDir: null,
                    buildDirs: buildDirs,
                    importPaths: [],
                    files: []),
                qmlLanguageServer: new CoreQmlMetadata.QmlLanguageServerSection(
                    disableCMakeCalls: true));
        }

        // Replicates QmllsBuildIniPatcher.BuildSectionKey for use in assertions.
        private static string SectionKey(string path)
        {
            var normalized = path.Replace('\\', '/').TrimEnd('/');
            if (normalized.Length >= 2 && normalized[1] == ':')
                normalized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
            return "[" + normalized.Replace("/", "<SLASH>") + "]";
        }

        private static string Normalize(string path) => path.Replace('\\', '/');

        // Minimal V2 ini with one generated workspace and an optional alias.
        private static string V2Ini(
            string sourcePath,
            string? aliasDir = null,
            string generatedImportPaths = "")
        {
            var lines = new List<string>
            {
                "[workspaces]",
                $"1\\sourcePath=\"{Normalize(sourcePath)}\"",
                $"1\\importPaths=\"{generatedImportPaths}\"",
                "1\\resourceFiles=\"\""
            };
            if (aliasDir != null) {
                lines.Add($"2\\sourcePath=\"{Normalize(aliasDir)}\"");
                lines.Add("2\\importPaths=\"\"");
                lines.Add("2\\resourceFiles=\"\"");
                lines.Add("size=2");
            } else {
                lines.Add("size=1");
            }
            return string.Join(Environment.NewLine, lines);
        }

        // Minimal V1 ini with one section containing a non-empty importPaths line.
        private static string V1Ini(string sectionDir) =>
            string.Join(Environment.NewLine, SectionKey(sectionDir), "importPaths=\"\"");

        private sealed class TestLog : IExtensionLog
        {
            public void Verbose(string message) { }
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message, Exception? exception = null) { }
        }
    }
}
