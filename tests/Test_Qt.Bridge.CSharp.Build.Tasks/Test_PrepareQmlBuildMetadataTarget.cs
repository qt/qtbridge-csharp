// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.Security;
using System.Text.Json;
using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_PrepareQmlBuildMetadataTarget : TestBase
    {
        [TestMethod]
        public void QtBridgeBuild_InvokesQmllsMetadataPreparation()
        {
            var projectPath = WriteProject(generatedWorkspaceExists: true);

            var result = RunMsBuild(projectPath);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            var qtDirectory = Path.Combine(TempDirectory, "native", "build", ".qt");
            var iniPath = Path.Combine(qtDirectory, QmllsBuildIniPatcher.FileName);
            var qrcPath = Path.Combine(qtDirectory, ProjectSourcesQrcWriter.FileName);
            var markerPath = Path.Combine(qtDirectory, BuildReadyMarker.FileName);
            Assert.IsTrue(File.Exists(qrcPath), result.Output);
            Assert.IsTrue(File.Exists(markerPath), result.Output);
            Assert.AreEqual("{\"version\":1}", File.ReadAllText(markerPath));
            Assert.Contains(Normalize(TempDirectory), File.ReadAllText(iniPath));
            Assert.Contains(Normalize(qrcPath), File.ReadAllText(iniPath));
        }

        [TestMethod]
        public void QtBridgeBuild_ReportsTaskDiagnosticWithoutPublishingReadyMarker()
        {
            var projectPath = WriteProject(generatedWorkspaceExists: false);

            var result = RunMsBuild(projectPath);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            Assert.Contains("generated workspace", result.Output);
            Assert.DoesNotContain("MSB4018", result.Output);
            Assert.IsFalse(File.Exists(Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                BuildReadyMarker.FileName)));
        }

        [TestMethod]
        public void QtBridgeBuild_RemovesStaleProjectQrcWithoutQmlFiles()
        {
            var projectPath = WriteProject(
                generatedWorkspaceExists: true,
                includeQml: false);
            var staleQrcPath = Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                ProjectSourcesQrcWriter.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(staleQrcPath)!);
            File.WriteAllText(staleQrcPath, "<RCC />");

            var result = RunMsBuild(projectPath);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            Assert.IsFalse(File.Exists(staleQrcPath));
            var iniPath = Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                QmllsBuildIniPatcher.FileName);
            Assert.DoesNotContain(ProjectSourcesQrcWriter.FileName, File.ReadAllText(iniPath));
            Assert.IsTrue(File.Exists(Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                BuildReadyMarker.FileName)));
        }

        [TestMethod]
        public void QtBridgeBuild_RefreshesReadyMarkerTimestamp()
        {
            var projectPath = WriteProject(generatedWorkspaceExists: true);
            var firstResult = RunMsBuild(projectPath);
            Assert.AreEqual(0, firstResult.ExitCode, firstResult.Output);
            var markerPath = Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                BuildReadyMarker.FileName);
            var oldTimestamp = DateTime.UtcNow.AddMinutes(-5);
            File.SetLastWriteTimeUtc(markerPath, oldTimestamp);

            var secondResult = RunMsBuild(projectPath);

            Assert.AreEqual(0, secondResult.ExitCode, secondResult.Output);
            Assert.IsTrue(File.GetLastWriteTimeUtc(markerPath) > oldTimestamp);
        }

        [TestMethod]
        public void QtBridgeBuild_InvalidatesReadyMarkerBeforeNativeBuild()
        {
            var projectPath = WriteProject(generatedWorkspaceExists: true);
            var markerPath = Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                BuildReadyMarker.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
            File.WriteAllText(markerPath, "stale");

            var result = RunMsBuild(projectPath);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            Assert.AreEqual("{\"version\":1}", File.ReadAllText(markerPath));
        }

        [TestMethod]
        public void QtBridgeBuild_LeavesMarkerAbsentWhenQmlTypesAreMissing()
        {
            var projectPath = WriteProject(
                generatedWorkspaceExists: true,
                includeQmlTypes: false);

            var result = RunMsBuild(projectPath);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            Assert.Contains("contains no .qmltypes files", result.Output);
            Assert.IsFalse(File.Exists(Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                BuildReadyMarker.FileName)));
        }

        [TestMethod]
        public void QtValidationFailure_InvalidatesReadyMarker()
        {
            var projectPath = WriteProject(
                generatedWorkspaceExists: true,
                validQt: false);
            var markerPath = Path.Combine(
                TempDirectory,
                "native",
                "build",
                ".qt",
                BuildReadyMarker.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
            File.WriteAllText(markerPath, "stale");

            var result = RunMsBuild(projectPath, "QtBridgeValidateQt");

            Assert.AreNotEqual(0, result.ExitCode);
            Assert.IsFalse(File.Exists(markerPath));
        }

        [TestMethod]
        public void QtBridgeWriteQmlMetadataFile_ExposesQmllsArtifacts()
        {
            var projectPath = WriteProject(generatedWorkspaceExists: true);

            var result = RunMsBuild(projectPath, "QtBridgeWriteQmlMetadataFile");

            Assert.AreEqual(0, result.ExitCode, result.Output);
            var metadataPath = Directory
                .EnumerateFiles(
                    Path.Combine(TempDirectory, "obj"),
                    "qtbridge-qml.ide.json",
                    SearchOption.AllDirectories)
                .Single();
            using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
            AssertMetadataPath(document.RootElement.GetProperty("projectFile").GetString()!);
            var application = document.RootElement.GetProperty("application");
            AssertMetadataPath(application.GetProperty("managedOutputDir").GetString()!);
            AssertMetadataPath(application.GetProperty("managedHostPath").GetString()!);
            AssertMetadataPath(application.GetProperty("nativeHostPath").GetString()!);
            var qml = document.RootElement.GetProperty("qml");
            AssertMetadataPath(qml.GetProperty("sourceDir").GetString()!);
            AssertMetadataPath(qml.GetProperty("projectSourceDir").GetString()!);
            AssertMetadataPath(qml.GetProperty("buildDirs")[0].GetString()!);
            AssertMetadataPath(qml.GetProperty("importPaths")[0].GetString()!);
            AssertMetadataPath(qml.GetProperty("importPaths")[1].GetString()!);
            var qmlls = document.RootElement.GetProperty("qmlLanguageServer");
            AssertMetadataPath(qmlls.GetProperty("readyFile").GetString()!);
            AssertMetadataPath(qmlls.GetProperty("buildIni").GetString()!);
            AssertMetadataPath(qmlls.GetProperty("projectSourcesQrc").GetString()!);
            var buildDirectory = Path.Combine(TempDirectory, "native", "build");
            var executableName = OperatingSystem.IsWindows()
                ? "TargetTestApp.exe"
                : "TargetTestApp";
            Assert.AreEqual(
                "TargetTestApp",
                application.GetProperty("assemblyName").GetString());
            Assert.AreEqual(
                executableName,
                application.GetProperty("executableName").GetString());
            Assert.AreEqual(
                NormalizeHostPath(Path.Combine(
                    TempDirectory,
                    "bin",
                    "Debug",
                    "net8.0") + Path.DirectorySeparatorChar),
                NormalizeHostPath(application.GetProperty("managedOutputDir").GetString()!));
            Assert.AreEqual(
                NormalizeHostPath(Path.Combine(
                    TempDirectory,
                    "bin",
                    "Debug",
                    "net8.0",
                    executableName)),
                NormalizeHostPath(application.GetProperty("managedHostPath").GetString()!));
            Assert.AreEqual(
                NormalizeHostPath(Path.Combine(TempDirectory, "native", "bin", executableName)),
                NormalizeHostPath(application.GetProperty("nativeHostPath").GetString()!));
            Assert.AreEqual(
                NormalizeHostPath(Path.Combine(
                    buildDirectory,
                    ".qt",
                    BuildReadyMarker.FileName)),
                NormalizeHostPath(qmlls.GetProperty("readyFile").GetString()!));
            Assert.AreEqual(
                NormalizeHostPath(Path.Combine(
                    buildDirectory,
                    ".qt",
                    QmllsBuildIniPatcher.FileName)),
                NormalizeHostPath(qmlls.GetProperty("buildIni").GetString()!));
            Assert.AreEqual(
                NormalizeHostPath(Path.Combine(
                    buildDirectory,
                    ".qt",
                   ProjectSourcesQrcWriter.FileName)),
                NormalizeHostPath(qmlls.GetProperty("projectSourcesQrc").GetString()!));
        }

        [TestMethod]
        public void QtBridgeProps_DeclaresQmlBuildMetadataUpToDateArtifactsForQmlProjects()
        {
            var projectPath = WriteUpToDateProject(includeQml: true);

            var result = RunMsBuild(projectPath, "DumpUpToDateCheckItems");

            Assert.AreEqual(0, result.ExitCode, result.Output);
            var inputs = ReadUpToDateItems("uptodate-inputs.txt");
            var outputs = ReadUpToDateItems("uptodate-outputs.txt");
            var metadataDir = NormalizeHostPath(Path.Combine(
                TempDirectory,
                "obj",
                "qt",
                "native",
                "build",
                ".qt"));
            CollectionAssert.Contains(
                inputs["QtBridgeQmlBuildMetadata"].ToArray(),
                NormalizeHostPath(Path.Combine(TempDirectory, "Main.qml")));
            CollectionAssert.Contains(
                inputs["QtBridgeQmlBuildMetadata"].ToArray(),
                NormalizeHostPath(projectPath));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    NormalizeHostPath(Path.Combine(metadataDir, QmllsBuildIniPatcher.FileName)),
                    NormalizeHostPath(Path.Combine(metadataDir, BuildReadyMarker.FileName)),
                    NormalizeHostPath(Path.Combine(metadataDir, ProjectSourcesQrcWriter.FileName))
                },
                outputs["QtBridgeQmlBuildMetadata"].ToArray());
        }

        [TestMethod]
        public void QtBridgeProps_OmitsProjectSourcesQrcUpToDateOutputWithoutQmlFiles()
        {
            var projectPath = WriteUpToDateProject(includeQml: false);

            var result = RunMsBuild(projectPath, "DumpUpToDateCheckItems");

            Assert.AreEqual(0, result.ExitCode, result.Output);
            var inputs = ReadUpToDateItems("uptodate-inputs.txt");
            var outputs = ReadUpToDateItems("uptodate-outputs.txt");
            var metadataDir = NormalizeHostPath(Path.Combine(
                TempDirectory,
                "obj",
                "qt",
                "native",
                "build",
                ".qt"));
            CollectionAssert.AreEquivalent(
                new[] { NormalizeHostPath(projectPath) },
                inputs["QtBridgeQmlBuildMetadata"].ToArray());
            CollectionAssert.AreEquivalent(
                new[]
                {
                    NormalizeHostPath(Path.Combine(metadataDir, QmllsBuildIniPatcher.FileName)),
                    NormalizeHostPath(Path.Combine(metadataDir, BuildReadyMarker.FileName))
                },
                outputs["QtBridgeQmlBuildMetadata"].ToArray());
            CollectionAssert.DoesNotContain(
                outputs["QtBridgeQmlBuildMetadata"].ToArray(),
                NormalizeHostPath(Path.Combine(metadataDir, ProjectSourcesQrcWriter.FileName)));
        }

        protected override string TempDirectoryName => "qtbridge-qmlls-target-tests";

        private static void AssertMetadataPath(string path) => Assert.DoesNotContain('\\', path);

        private string WriteProject(
            bool generatedWorkspaceExists,
            bool includeQml = true,
            bool includeQmlTypes = true,
            bool validQt = true)
        {
            Directory.CreateDirectory(TempDirectory);
            if (includeQml) {
                var qmlDirectory = Path.Combine(TempDirectory, "Views");
                Directory.CreateDirectory(qmlDirectory);
                File.WriteAllText(Path.Combine(qmlDirectory, "Details.qml"), "");
            }

            var repositoryRoot = FindRepositoryRoot();
            var targetsPath = Path.Combine(repositoryRoot, "build", "Qt.Bridge.targets");
            var taskAssembly = typeof(PrepareQmlBuildMetadata).Assembly.Location;
            var qtDirectory = Path.Combine(TempDirectory, "qt");
            if (validQt) {
                var qtConfigDirectory = Path.Combine(qtDirectory, "lib", "cmake", "Qt6");
                Directory.CreateDirectory(qtConfigDirectory);
                File.WriteAllText(Path.Combine(qtConfigDirectory, "Qt6Config.cmake"), "");
            }
            var qtPath = validQt ? qtDirectory : Path.Combine(TempDirectory, "missing-qt");
            var sourceExpression = generatedWorkspaceExists
                ? "$(_QtFixtureSourceDir)"
                : "$(_QtFixtureSourceDir)/missing";
            var projectPath = Path.Combine(TempDirectory, "TargetTest.proj");
            File.WriteAllText(projectPath,
                $"""
                <Project>
                  <PropertyGroup>
                    <Configuration>Debug</Configuration>
                    <DesignTimeBuild>false</DesignTimeBuild>
                    <AssemblyName>TargetTestApp</AssemblyName>
                    <TargetFramework>net8.0</TargetFramework>
                    <BaseOutputPath>bin/</BaseOutputPath>
                    <BaseIntermediateOutputPath>obj/</BaseIntermediateOutputPath>
                    <IntermediateOutputPath>obj/Debug/</IntermediateOutputPath>
                    <TargetDir>$(MSBuildProjectDirectory)/bin/Debug/net8.0/</TargetDir>
                    <ProjectDir>{XmlEscape(TempDirectory + Path.DirectorySeparatorChar)}</ProjectDir>
                    <QtNativeBuildDir>native/build</QtNativeBuildDir>
                    <QtNativeSourceDir>native/source</QtNativeSourceDir>
                    <QtNativeBinDir>native/bin</QtNativeBinDir>
                    <QtDir>{XmlEscape(qtPath)}</QtDir>
                    <QtBridgeBuildTasks>{XmlEscape(taskAssembly)}</QtBridgeBuildTasks>
                  </PropertyGroup>
                  <Import Project="{XmlEscape(targetsPath)}" />
                  <Target Name="QtBridgeBuild">
                    <Error
                      Condition="Exists(
                        '$(MSBuildProjectDirectory)/$(QtNativeBuildDir)/.qt/{BuildReadyMarker.FileName}')"
                      Text="The ready marker was not invalidated before QtBridgeBuild." />
                    <PropertyGroup>
                      <_QtFixtureBuildDir>$([System.IO.Path]::GetFullPath(
                        '$(MSBuildProjectDirectory)/$(QtNativeBuildDir)'))</_QtFixtureBuildDir>
                      <_QtFixtureSourceDir>$([System.IO.Path]::GetFullPath(
                        '$(MSBuildProjectDirectory)/$(QtNativeSourceDir)'))</_QtFixtureSourceDir>
                    </PropertyGroup>
                    <MakeDir
                      Directories="$(_QtFixtureBuildDir)/.qt;
                                   $(_QtFixtureBuildDir)/Application;
                                   $(_QtFixtureSourceDir)" />
                    <ItemGroup>
                      <_QtFixtureIniLine Include="[workspaces]" />
                      <_QtFixtureIniLine
                        Include="1\sourcePath=&quot;{sourceExpression}&quot;" />
                      <_QtFixtureIniLine Include="1\importPaths=&quot;/generated/imports&quot;" />
                      <_QtFixtureIniLine Include="1\resourceFiles=&quot;&quot;" />
                      <_QtFixtureIniLine Include="size=1" />
                    </ItemGroup>
                    <WriteLinesToFile
                      File="$(_QtFixtureBuildDir)/.qt/{QmllsBuildIniPatcher.FileName}"
                      Lines="@(_QtFixtureIniLine)"
                      Overwrite="true" />
                    <WriteLinesToFile
                      Condition="'{includeQmlTypes}' == 'True'"
                      File="$(_QtFixtureBuildDir)/Application/Application.qmltypes"
                      Lines="import QtQuick.tooling 1.2"
                      Overwrite="true" />
                  </Target>
                </Project>
                """);
            return projectPath;
        }

        private string WriteUpToDateProject(bool includeQml)
        {
            Directory.CreateDirectory(TempDirectory);
            if (includeQml)
                File.WriteAllText(Path.Combine(TempDirectory, "Main.qml"), "");

            var repositoryRoot = FindRepositoryRoot();
            var propsPath = Path.Combine(repositoryRoot, "build", "Qt.Bridge.props");
            var projectPath = Path.Combine(TempDirectory, "UpToDate.proj");
            File.WriteAllText(projectPath,
                $"""
                <Project>
                  <PropertyGroup>
                    <BaseOutputPath>bin/</BaseOutputPath>
                    <BaseIntermediateOutputPath>obj/</BaseIntermediateOutputPath>
                    <IntermediateOutputPath>obj/</IntermediateOutputPath>
                  </PropertyGroup>
                  <Import Project="{XmlEscape(propsPath)}" />
                  <Target Name="DumpUpToDateCheckItems">
                    <WriteLinesToFile
                      File="$(MSBuildProjectDirectory)\uptodate-inputs.txt"
                      Overwrite="true"
                      Lines="@(UpToDateCheckInput->'%(Set)|%(Identity)', '&#x0A;')" />
                    <WriteLinesToFile
                      File="$(MSBuildProjectDirectory)\uptodate-outputs.txt"
                      Overwrite="true"
                      Lines="@(UpToDateCheckOutput->'%(Set)|%(Identity)', '&#x0A;')" />
                  </Target>
                </Project>
                """);
            return projectPath;
        }

        private static BuildResult RunMsBuild(
            string projectPath,
            string target = "QtBridgeBuild")
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("msbuild");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("/t:" + target);
            startInfo.ArgumentList.Add("/nologo");
            startInfo.ArgumentList.Add("/v:minimal");

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start dotnet msbuild.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new BuildResult(process.ExitCode, output + error);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null) {
                if (File.Exists(Path.Combine(directory.FullName, "build", "Qt.Bridge.targets")))
                    return directory.FullName;
                directory = directory.Parent;
            }
            throw new InvalidOperationException("Could not locate the repository root.");
        }

        private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? "";

        private static string NormalizeHostPath(string path) =>
            Path.GetFullPath(PathUtilities.ToHostSeparators(path));

        private Dictionary<string, List<string>> ReadUpToDateItems(string fileName)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(Path.Combine(TempDirectory, fileName))) {
                var separator = line.IndexOf('|');
                if (separator <= 0 || separator == line.Length - 1)
                    continue;

                var set = line[..separator];
                var rawPath = line[(separator + 1)..];
                var path = Path.IsPathRooted(rawPath)
                    ? NormalizeHostPath(rawPath)
                    : NormalizeHostPath(Path.Combine(TempDirectory, rawPath));
                if (!result.TryGetValue(set, out var paths)) {
                    paths = [];
                    result[set] = paths;
                }
                paths.Add(path);
            }
            return result;
        }

        private sealed record BuildResult(int ExitCode, string Output);
    }
}
