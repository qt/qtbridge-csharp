// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.Security;
using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_NativeDeployment : TestBase
    {
        [TestMethod]
        public void CMakeDeploymentDoesNotCopyTheNativeHostToTheApplicationOutput()
        {
            var cmakeDirectory = Path.Combine(TempDirectory, "tools");
            var projectPath = WriteProject();
            WriteFakeCMake(cmakeDirectory);

            var result = RunMsBuild(projectPath, cmakeDirectory);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            var output = Path.Combine(TempDirectory, "output");
            Assert.IsFalse(File.Exists(Path.Combine(output, "TargetTestApp")));
            Assert.IsTrue(File.Exists(Path.Combine(output, "plugins", "platforms.txt")));

            var staged = Path.Combine(TempDirectory, "native", "build", "deploy");
            Assert.IsFalse(File.Exists(Path.Combine(staged, "TargetTestApp")));
            var deployed = File.ReadAllText(Path.Combine(output, "qtdeploy.txt"));
            Assert.Contains("plugins", deployed);
            Assert.DoesNotContain("TargetTestApp", deployed);
        }

        [TestMethod]
        public void QmlDeploymentRemovesFilesDeletedFromTheProject()
        {
            var project = WriteQmlProject();
            var source = Path.Combine(TempDirectory, "Page.qml");
            File.WriteAllText(source, "import QtQuick");

            var first = RunMsBuild(project, "RunQmlDeployment", null);

            Assert.AreEqual(0, first.ExitCode, first.Output);
            var staged = Path.Combine(TempDirectory, "native", "source", "qml", "Application",
                "Page.qml");
            var deployed = Path.Combine(TempDirectory, "output", "Application", "Page.qml");
            Assert.IsTrue(File.Exists(staged), first.Output);
            Assert.IsTrue(File.Exists(deployed));

            File.Delete(source);
            var second = RunMsBuild(project, "RunQmlDeployment", null);

            Assert.AreEqual(0, second.ExitCode, second.Output);
            Assert.IsFalse(File.Exists(staged));
            Assert.IsFalse(File.Exists(deployed));
        }

        [TestMethod]
        public void QmlGeneratedSourcesSupportARedirectedIntermediateOutputPath()
        {
            var redirectedIntermediateOutput = Path.Combine(TempDirectory, "redirected", "obj")
                + Path.DirectorySeparatorChar;
            var project = WriteQmlProject(redirectedIntermediateOutput,
                useDefaultNativeDirectories: true);
            File.WriteAllText(Path.Combine(TempDirectory, "Page.qml"), "import QtQuick");

            var result = RunMsBuild(project, "RunQmlDeployment", null);

            Assert.AreEqual(0, result.ExitCode, result.Output);
            Assert.IsTrue(File.Exists(Path.Combine(redirectedIntermediateOutput, "qt", "csharp",
                "QmlFiles.cs")), result.Output);
        }

        protected override string TempDirectoryName => "qtbridge-native-deployment-tests";

        private string WriteProject()
        {
            Directory.CreateDirectory(TempDirectory);
            var nativeHost = Path.Combine(TempDirectory, "native", "bin", "TargetTestApp");
            Directory.CreateDirectory(Path.GetDirectoryName(nativeHost)!);
            File.WriteAllText(nativeHost, "native host");

            var targets = Path.Combine(FindRepositoryRoot(), "build", "Qt.Bridge.targets");
            var taskAssembly = typeof(PrepareQmlBuildMetadata).Assembly.Location;
            var project = Path.Combine(TempDirectory, "NativeDeployment.proj");
            File.WriteAllText(project, $"""
                <Project>
                  <PropertyGroup>
                    <DesignTimeBuild>false</DesignTimeBuild>
                    <QtGenSourceCode>true</QtGenSourceCode>
                    <QtHostIsWindows>false</QtHostIsWindows>
                    <QtHostIsLinux>true</QtHostIsLinux>
                    <QtHostIsMacOS>false</QtHostIsMacOS>
                    <QtNativeBuildDir>native/build</QtNativeBuildDir>
                    <QtNativeHostPath>{XmlEscape(nativeHost)}</QtNativeHostPath>
                    <TargetDir>$(MSBuildProjectDirectory)/output/</TargetDir>
                    <QtBridgeBuildTasks>{XmlEscape(taskAssembly)}</QtBridgeBuildTasks>
                  </PropertyGroup>
                  <Import Project="{XmlEscape(targets)}" />
                  <Target Name="QtBridgeBuild" />
                </Project>
                """);
            return project;
        }

        private static void WriteFakeCMake(string directory)
        {
            Directory.CreateDirectory(directory);
            if (OperatingSystem.IsWindows()) {
                File.WriteAllText(Path.Combine(directory, "cmake.cmd"), """
                    @echo off
                    mkdir "%~4\plugins"
                    > "%~4\TargetTestApp" echo unpatched native host
                    > "%~4\plugins\platforms.txt" echo deployed runtime
                    """);
                return;
            }

            var path = Path.Combine(directory, "cmake");
            File.WriteAllText(path, """
                #!/bin/sh
                mkdir -p "$4/plugins"
                printf 'unpatched native host' > "$4/TargetTestApp"
                printf 'deployed runtime' > "$4/plugins/platforms.txt"
                """);
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute);
        }

        private string WriteQmlProject(
            string intermediateOutputPath = "obj/",
            bool useDefaultNativeDirectories = false)
        {
            Directory.CreateDirectory(TempDirectory);
            var props = Path.Combine(FindRepositoryRoot(), "build", "Qt.Bridge.props");
            var targets = Path.Combine(FindRepositoryRoot(), "build", "Qt.Bridge.targets");
            var project = Path.Combine(TempDirectory, "QmlDeployment.proj");
            File.WriteAllText(project, $"""
                <Project>
                  {(useDefaultNativeDirectories ? "" : """
                  <PropertyGroup>
                    <QtNativeSourceDir>native/source</QtNativeSourceDir>
                  </PropertyGroup>
                  """)}
                  <Import Project="{XmlEscape(props)}" />
                  <PropertyGroup>
                    <DesignTimeBuild>false</DesignTimeBuild>
                    <ProjectDir>$(MSBuildProjectDirectory)/</ProjectDir>
                    <BaseIntermediateOutputPath>{XmlEscape(intermediateOutputPath)}</BaseIntermediateOutputPath>
                    <IntermediateOutputPath>{XmlEscape(intermediateOutputPath)}</IntermediateOutputPath>
                    <TargetDir>$(MSBuildProjectDirectory)/output/</TargetDir>
                  </PropertyGroup>
                  <Import Project="{XmlEscape(targets)}" />
                  <Target Name="AddFixtureQml" BeforeTargets="QtBridgeSetupQml">
                    <ItemGroup>
                      <Qml Include="Page.qml"
                        Condition="Exists('$(MSBuildProjectDirectory)/Page.qml')" />
                    </ItemGroup>
                  </Target>
                  <Target Name="RunQmlDeployment"
                    DependsOnTargets="QtBridgeAddQmlFiles;QtBridgeDeployQml" />
                </Project>
                """);
            return project;
        }

        private static BuildResult RunMsBuild(string project, string cmakeDirectory) =>
            RunMsBuild(project, "QtBridgeDeploy_SourceCode_Linux_MacOS", null, cmakeDirectory);

        private static BuildResult RunMsBuild(string project, string target, string? property) =>
            RunMsBuild(project, target, property, null);

        private static BuildResult RunMsBuild(
            string project,
            string target,
            string? property,
            string? cmakeDirectory)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            if (cmakeDirectory != null) {
                info.Environment["PATH"] = cmakeDirectory + Path.PathSeparator
                    + info.Environment["PATH"];
            }
            info.ArgumentList.Add("msbuild");
            info.ArgumentList.Add(project);
            info.ArgumentList.Add("/t:" + target);
            if (property != null)
                info.ArgumentList.Add(property);
            info.ArgumentList.Add("/nologo");
            info.ArgumentList.Add("/v:minimal");

            using var process = Process.Start(info)
                ?? throw new InvalidOperationException("Failed to start dotnet msbuild.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new BuildResult(process.ExitCode, output + error);
        }

        private sealed record BuildResult(int ExitCode, string Output);
    }
}
