// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Qt.Bridge.CSharp.Build.Tasks;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public sealed class Test_QmlBuildMetadata
    {
        private const string ProgramCs =
            """
                using System;

                namespace Test_QmlBuildMetadata;

                internal static class Program
                {
                    private static int Main(string[] args)
                    {
                        Console.WriteLine("Qml build metadata");
                        return 0;
                    }
                }
            """;

        private const string MainQml =
            """
                import QtQuick

                ApplicationWindow {
                    width: 320
                    height: 240
                    visible: true
                }
            """;

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Build_PublishesCompleteQmlBuildMetadataArtifacts()
        {
            using var temp = CreateProject();

            var (buildOk, buildOutput) = await temp.BuildAsync();

            temp.SaveLog();
            Assert.IsTrue(buildOk, buildOutput);

            var artifacts = await ResolveArtifactsAsync(temp);
            Assert.IsTrue(File.Exists(artifacts.MetadataPath), artifacts.MetadataPath);
            Assert.IsTrue(File.Exists(artifacts.BuildIniPath), artifacts.BuildIniPath);
            Assert.IsTrue(File.Exists(artifacts.ReadyMarkerPath), artifacts.ReadyMarkerPath);
            Assert.IsTrue(File.Exists(artifacts.ProjectSourcesQrcPath),
                artifacts.ProjectSourcesQrcPath);
            Assert.AreEqual("{\"version\":1}", await File.ReadAllTextAsync(artifacts.ReadyMarkerPath,
                    TestContext.CancellationTokenSource.Token));

            var buildIni = await File.ReadAllTextAsync(artifacts.BuildIniPath,
                TestContext.CancellationTokenSource.Token);
            AssertBuildIniContainsDirectory(buildIni, artifacts.ProjectDirectory);
            AssertBuildIniContainsFile(buildIni, artifacts.ProjectSourcesQrcPath);

            var qrc = await File.ReadAllTextAsync(artifacts.ProjectSourcesQrcPath,
                TestContext.CancellationTokenSource.Token);
            Assert.Contains("alias=\"Main.qml\"", qrc);
            Assert.Contains("prefix=\"/qt/qml/", qrc);

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(
                artifacts.MetadataPath, TestContext.CancellationTokenSource.Token));
            var root = metadata.RootElement;
            var qml = root.GetProperty("qml");
            var qmlls = root.GetProperty("qmlLanguageServer");

            AssertSameDirectory(
                artifacts.ProjectDirectory,
                qml.GetProperty("projectSourceDir").GetString()!);
            AssertSameDirectory(
                artifacts.BuildDirectory,
                qml.GetProperty("buildDirs")[0].GetString()!);
            AssertSameFilePath(
                artifacts.ReadyMarkerPath,
                qmlls.GetProperty("readyFile").GetString()!);
            AssertSameFilePath(
                artifacts.BuildIniPath,
                qmlls.GetProperty("buildIni").GetString()!);
            AssertSameFilePath(
                artifacts.ProjectSourcesQrcPath,
                qmlls.GetProperty("projectSourcesQrc").GetString()!);
        }

        [TestMethod]
        public async Task Build_RecreatesDeletedQmlBuildMetadataArtifacts()
        {
            using var temp = CreateProject();
            var (buildOk, buildOutput) = await temp.BuildAsync();
            Assert.IsTrue(buildOk, buildOutput);

            var artifacts = await ResolveArtifactsAsync(temp);
            File.Delete(artifacts.BuildIniPath);
            File.Delete(artifacts.ProjectSourcesQrcPath);
            File.Delete(artifacts.ReadyMarkerPath);

            var (secondBuildOk, secondBuildOutput) = await temp.BuildAsync();

            temp.SaveLog("repair");
            Assert.IsTrue(secondBuildOk, secondBuildOutput);
            Assert.IsTrue(File.Exists(artifacts.BuildIniPath), artifacts.BuildIniPath);
            Assert.IsTrue(File.Exists(artifacts.ProjectSourcesQrcPath),
                artifacts.ProjectSourcesQrcPath);
            Assert.IsTrue(File.Exists(artifacts.ReadyMarkerPath), artifacts.ReadyMarkerPath);
        }

        [TestMethod]
        public async Task Rebuild_RefreshesReadyMarkerTimestamp()
        {
            using var temp = CreateProject();
            var (buildOk, buildOutput) = await temp.BuildAsync();
            Assert.IsTrue(buildOk, buildOutput);

            var artifacts = await ResolveArtifactsAsync(temp);
            var oldTimestamp = DateTime.UtcNow.AddMinutes(-5);
            File.SetLastWriteTimeUtc(artifacts.ReadyMarkerPath, oldTimestamp);

            var (rebuildOk, rebuildOutput) = await temp.BuildAsync(new BuildOptions
            {
                Targets = ["Rebuild"]
            });

            temp.SaveLog("rebuild");
            Assert.IsTrue(rebuildOk, rebuildOutput);
            Assert.IsTrue(File.GetLastWriteTimeUtc(artifacts.ReadyMarkerPath) > oldTimestamp);
        }

        [TestMethod]
        public async Task Clean_RemovesQmlBuildMetadataArtifacts()
        {
            using var temp = CreateProject();
            var (buildOk, buildOutput) = await temp.BuildAsync();
            Assert.IsTrue(buildOk, buildOutput);

            var artifacts = await ResolveArtifactsAsync(temp);
            var (cleanOk, cleanOutput) = await temp.BuildAsync(new BuildOptions
            {
                Restore = false,
                Targets = ["Clean"],
                TargetPath = "",
                TargetExePath = ""
            });

            Assert.IsTrue(cleanOk, cleanOutput);
            Assert.IsFalse(File.Exists(artifacts.BuildIniPath), artifacts.BuildIniPath);
            Assert.IsFalse(File.Exists(artifacts.ProjectSourcesQrcPath),
                artifacts.ProjectSourcesQrcPath);
            Assert.IsFalse(File.Exists(artifacts.ReadyMarkerPath), artifacts.ReadyMarkerPath);
        }

        private static TempProject CreateProject()
        {
            const string root = "$(BaseIntermediateOutputPath)$(Platform)/$(Configuration)"
                + "/$(TargetFramework)";

            var temp = new TempProject();
            temp.Create(new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                BeforeSdkProps = """
                   <PropertyGroup>
                     <BaseOutputPath>bin/</BaseOutputPath>
                     <BaseIntermediateOutputPath>obj/</BaseIntermediateOutputPath>
                   </PropertyGroup>
                 """,
                BeforeSdkTargets = $"""
                  <PropertyGroup>
                    <QtNativeBuildDir>{root}/qt/native/build</QtNativeBuildDir>
                    <QtNativeSourceDir>{root}/qt/native/source</QtNativeSourceDir>
                    <QtNativeBinDir>{root}/qt/native/bin</QtNativeBinDir>
                  </PropertyGroup>
                 """
            });
            temp.AddFile("Program.cs", ProgramCs);
            temp.AddFile("Main.qml", MainQml);
            return temp;
        }

        private static async Task<BuildArtifacts> ResolveArtifactsAsync(TempProject temp)
        {
            var buildDirProperty = await temp.GetPropertyAsync("QtNativeBuildDir");
            Assert.IsFalse(string.IsNullOrWhiteSpace(buildDirProperty));
            var buildDirectory = Path.GetFullPath(Path.Combine(temp.ProjectDir, buildDirProperty!));

            var metadataPath = Directory
                .EnumerateFiles(temp.ProjectDir,
                    QmlMetadataReader.MetadataFileName, SearchOption.AllDirectories)
                .Single();

            return new BuildArtifacts(
                temp.ProjectDir,
                buildDirectory,
                metadataPath,
                Path.Combine(buildDirectory, ".qt", QmllsBuildIniPatcher.FileName),
                Path.Combine(buildDirectory, ".qt", ProjectSourcesQrcWriter.FileName),
                Path.Combine(buildDirectory, ".qt", BuildReadyMarker.FileName));
        }

        private static void AssertSameFilePath(string expected, string actual)
        {
            Assert.AreEqual(Path.GetFileName(expected), Path.GetFileName(actual));
            AssertSameDirectory(Path.GetDirectoryName(expected)!, Path.GetDirectoryName(actual)!);
        }

        private static void AssertSameDirectory(string expected, string actual)
        {
            Assert.IsTrue(Directory.Exists(expected), expected);
            Assert.IsTrue(Directory.Exists(actual), actual);
            Assert.IsTrue(SameDirectory(expected, actual),
                $"'{expected}' and '{actual}' do not identify the same directory.");
        }

        // macOS can expose the same directory through more than one path (e.g. '/var' and
        // '/private/var'), so identity is verified by writing a marker file and checking that it
        // is visible through both paths, rather than by comparing path text.
        private static bool SameDirectory(string expected, string actual)
        {
            var expectedPath = Path.GetFullPath(expected);
            var actualPath = Path.GetFullPath(actual);
            if (OperatingSystem.IsWindows()
                && string.Equals(expectedPath, actualPath, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            if (!Directory.Exists(expectedPath) || !Directory.Exists(actualPath))
                return false;

            var markerName = $".qtbridge-path-{Guid.NewGuid():N}";
            var expectedMarker = Path.Combine(expectedPath, markerName);
            var actualMarker = Path.Combine(actualPath, markerName);
            try {
                File.WriteAllText(expectedMarker, string.Empty);
                return File.Exists(actualMarker);
            } finally {
                File.Delete(expectedMarker);
            }
        }

        private static string DecodeIniPath(string path) =>
            path.Replace("<SLASH>", "/").Trim();

        // Qt has used both encoded directory section names and quoted workspace values. Decode both
        // formats so callers can match by filesystem identity instead of literal path text.
        private static IEnumerable<string> ExtractIniPathCandidates(string ini)
        {
            var sectionPaths = Regex.Matches(ini, @"(?m)^\[([^\]\r\n]+)\]\s*$")
                .Select(match => DecodeIniPath(match.Groups[1].Value))
                .Where(Path.IsPathRooted);
            var quotedValues = Regex.Matches(ini, "=\"([^\"]*)\"").SelectMany(match =>
                match.Groups[1].Value
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(DecodeIniPath));
            return sectionPaths.Concat(quotedValues);
        }

        private static void AssertBuildIniContainsDirectory(string buildIni, string expectedDirectory)
        {
            Assert.IsTrue(
                ExtractIniPathCandidates(buildIni).Any(candidate =>
                    SameDirectory(expectedDirectory, candidate)),
                $"Directory '{expectedDirectory}' was not found among the paths in the build ini:"
                + $"{Environment.NewLine}{buildIni}");
        }

        private static void AssertBuildIniContainsFile(string buildIni, string expectedFilePath)
        {
            var expectedName = Path.GetFileName(expectedFilePath);
            var expectedDir = Path.GetDirectoryName(expectedFilePath)!;
            Assert.IsTrue(
                ExtractIniPathCandidates(buildIni).Any(candidate =>
                    string.Equals(Path.GetFileName(candidate), expectedName, StringComparison.Ordinal)
                    && SameDirectory(expectedDir, Path.GetDirectoryName(candidate) ?? "")),
                $"'{expectedFilePath}' was not found among the paths in the build ini:"
                + $"{Environment.NewLine}{buildIni}");
        }

        private sealed record BuildArtifacts(
            string ProjectDirectory,
            string BuildDirectory,
            string MetadataPath,
            string BuildIniPath,
            string ProjectSourcesQrcPath,
            string ReadyMarkerPath);
    }
}
