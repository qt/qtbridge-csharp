// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Text.Json;

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
            Assert.Contains(Normalize(artifacts.ProjectDirectory), buildIni);
            Assert.Contains(Normalize(artifacts.ProjectSourcesQrcPath), buildIni);

            var qrc = await File.ReadAllTextAsync(artifacts.ProjectSourcesQrcPath,
                TestContext.CancellationTokenSource.Token);
            Assert.Contains("alias=\"Main.qml\"", qrc);
            Assert.Contains("prefix=\"/qt/qml/", qrc);

            using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(
                artifacts.MetadataPath, TestContext.CancellationTokenSource.Token));
            var root = metadata.RootElement;
            var qml = root.GetProperty("qml");
            var qmlls = root.GetProperty("qmlLanguageServer");

            Assert.AreEqual(
                Normalize(artifacts.ProjectDirectory),
                Normalize(qml.GetProperty("projectSourceDir").GetString()!));
            Assert.AreEqual(
                Normalize(artifacts.BuildDirectory),
                Normalize(qml.GetProperty("buildDirs")[0].GetString()!));
            Assert.AreEqual(
                Normalize(artifacts.ReadyMarkerPath),
                Normalize(qmlls.GetProperty("readyFile").GetString()!));
            Assert.AreEqual(
                Normalize(artifacts.BuildIniPath),
                Normalize(qmlls.GetProperty("buildIni").GetString()!));
            Assert.AreEqual(
                Normalize(artifacts.ProjectSourcesQrcPath),
                Normalize(qmlls.GetProperty("projectSourcesQrc").GetString()!));
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
                .EnumerateFiles(temp.ProjectDir, "qtbridge-qml.ide.json", SearchOption.AllDirectories)
                .Single();

            return new BuildArtifacts(
                temp.ProjectDir,
                buildDirectory,
                metadataPath,
                Path.Combine(buildDirectory, ".qt", ".qmlls.build.ini"),
                Path.Combine(buildDirectory, ".qt", "qtbridge_project_sources.qrc"),
                Path.Combine(buildDirectory, ".qt", "qtbridge-build.ready"));
        }

        private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');

        private sealed record BuildArtifacts(
            string ProjectDirectory,
            string BuildDirectory,
            string MetadataPath,
            string BuildIniPath,
            string ProjectSourcesQrcPath,
            string ReadyMarkerPath);
    }
}
