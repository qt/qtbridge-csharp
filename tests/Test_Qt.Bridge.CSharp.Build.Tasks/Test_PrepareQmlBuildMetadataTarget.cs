// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.Security;
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
            Assert.IsTrue(File.Exists(qrcPath), result.Output);
            Assert.Contains(Normalize(TempDirectory), File.ReadAllText(iniPath));
            Assert.Contains(Normalize(qrcPath), File.ReadAllText(iniPath));
        }

        [TestMethod]
        public void QtBridgeBuild_ReportsTaskDiagnosticWithoutUnexpectedFailure()
        {
            var projectPath = WriteProject(generatedWorkspaceExists: false);

            var result = RunMsBuild(projectPath);

            Assert.AreEqual(0, result.ExitCode);
            Assert.Contains("generated workspace", result.Output);
            Assert.DoesNotContain("MSB4018", result.Output);
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
        }

        protected override string TempDirectoryName => "qtbridge-qmlls-target-tests";

        private string WriteProject(bool generatedWorkspaceExists, bool includeQml = true)
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
                    <BaseOutputPath>bin/</BaseOutputPath>
                    <BaseIntermediateOutputPath>obj/</BaseIntermediateOutputPath>
                    <IntermediateOutputPath>obj/Debug/</IntermediateOutputPath>
                    <QtBridgeBuildTasks>{XmlEscape(taskAssembly)}</QtBridgeBuildTasks>
                  </PropertyGroup>
                  <Import Project="{XmlEscape(targetsPath)}" />
                  <Target Name="QtBridgeBuild">
                    <PropertyGroup>
                      <QtNativeBuildDir>native/build</QtNativeBuildDir>
                      <QtNativeSourceDir>native/source</QtNativeSourceDir>
                      <_QtFixtureBuildDir>$([System.IO.Path]::GetFullPath(
                        '$(MSBuildProjectDirectory)/$(QtNativeBuildDir)'))</_QtFixtureBuildDir>
                      <_QtFixtureSourceDir>$([System.IO.Path]::GetFullPath(
                        '$(MSBuildProjectDirectory)/$(QtNativeSourceDir)'))</_QtFixtureSourceDir>
                    </PropertyGroup>
                    <MakeDir Directories="$(_QtFixtureBuildDir)/.qt;$(_QtFixtureSourceDir)" />
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
                  </Target>
                </Project>
                """);
            return projectPath;
        }

        private static BuildResult RunMsBuild(string projectPath)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("msbuild");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("/t:QtBridgeBuild");
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

        private sealed record BuildResult(int ExitCode, string Output);
    }
}
