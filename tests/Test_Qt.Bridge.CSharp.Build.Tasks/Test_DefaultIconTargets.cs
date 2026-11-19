// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public class Test_DefaultIconTargets : TestBase
    {
        protected override string TempDirectoryName => "qtbridge-default-icon-tests";

        [TestMethod]
        [DataRow("Exe", "", true)]
        [DataRow("WinExe", "", true)]
        [DataRow("Exe", "false", false)]
        [DataRow("Exe", "disable", false)]
        [DataRow("Library", "", false)]
        [DataRow("Library", "App.ico", false)]
        public async Task OnlyApplicationsSelectTheDefaultIcon(string outputType, string icon, bool enabled)
        {
            var project = WriteProject(outputType, icon);
            await Build(project, "QtBridgeAddResources");
            var resources = File.ReadAllText(Path.Combine(TempDirectory, "obj/qt/csharp/QtResources.cs"));
            Assert.Contains("icons/app.ico", resources);
            Assert.AreEqual(enabled, resources.Contains("qt/gui/qguiapplication/windowIcon"));
        }

        [TestMethod]
        public async Task StagedQmlPathsAreAbsolute()
        {
            var project = WriteProject("Exe", "false");
            File.WriteAllText(Path.Combine(TempDirectory, "Page.qml"), "import QtQuick\nQtObject {}");
            await Build(project, "QtBridgeAddQmlFiles");
            var source = File.ReadAllText(Path.Combine(TempDirectory, "obj/qt/csharp/QmlFiles.cs"));
            var path = Regex.Match(source, "FilePath = @\"([^\"]+)\"").Groups[1].Value;
            Assert.IsTrue(Path.IsPathFullyQualified(path), source);
            Assert.IsTrue(File.Exists(path), source);
        }

        private string WriteProject(string outputType, string icon)
        {
            Directory.CreateDirectory(TempDirectory);
            File.WriteAllText(Path.Combine(TempDirectory, "App.ico"), "resource fixture");
            var targets = Path.Combine(FindRepositoryRoot(), "build/Qt.Bridge.targets");
            var project = Path.Combine(TempDirectory, "Icon.proj");
            File.WriteAllText(project, $"""
                <Project>
                  <PropertyGroup>
                    <ProjectDir>$(MSBuildProjectDirectory)/</ProjectDir>
                    <OutputType>{outputType}</OutputType>
                    <QtDefaultIcon>{icon}</QtDefaultIcon>
                    <IntermediateOutputPath>obj/</IntermediateOutputPath>
                    <BaseIntermediateOutputPath>obj/</BaseIntermediateOutputPath>
                    <BaseOutputPath>bin/</BaseOutputPath>
                    <QtNativeSourceDir>native/source</QtNativeSourceDir>
                    <QtNativeBuildDir>native/build</QtNativeBuildDir>
                    <QtNativeBinDir>native/bin</QtNativeBinDir>
                  </PropertyGroup>
                  <ItemGroup><QtResource Include="App.ico"><Alias>icons/app.ico</Alias></QtResource></ItemGroup>
                  <Import Project="{XmlEscape(targets)}" />
                  <PropertyGroup><ApplicationIcon>App.ico</ApplicationIcon></PropertyGroup>
                </Project>
                """);
            return project;
        }

        private static async Task Build(string project, string target)
        {
            var info = new ProcessStartInfo("dotnet") {
                WorkingDirectory = Path.GetDirectoryName(project)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (var arg in new[] { "msbuild", project, "-nologo", "-v:minimal",
                    "-p:Platform=AnyCPU", "-t:" + target })
                info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try {
                await process.WaitForExitAsync(timeout.Token);
            } catch (OperationCanceledException) {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw;
            }
            Assert.AreEqual(0, process.ExitCode, await output + await error);
        }
    }
}
