// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_ResourcePackTargets : TestBase
    {
        public TestContext TestContext { get; set; }
        private CancellationToken Ct => TestContext.CancellationTokenSource.Token;

        protected override string TempDirectoryName => "qtbridge-resource-pack-tests";

        private string Native => Path.Combine(TempDirectory, "native");
        private string Output => Path.Combine(TempDirectory, "output");

        private string QrcPath => Path.Combine(Native, "qt_bridge_resources.qrc");

        private const string PackName = "qt_bridge_resources.rcc";
        private string PackPath => Path.Combine(Native, "build", PackName);
        private string DeployedPath => Path.Combine(Output, PackName);

        [TestMethod]
        public async Task ResourcePackBuildTracksBytesAndRepairsDeployment()
        {
            var rcc = FindRcc();
            var project = await WriteProjectAsync(rcc);
            var asset = await WriteQrcAsync();
            AssertSucceeded(await Build(project));
            var initialBytes = await File.ReadAllBytesAsync(PackPath, Ct);
            Assert.AreEqual("qres", System.Text.Encoding.ASCII.GetString(initialBytes, 0, 4));
            CollectionAssert.AreEqual(initialBytes, await File.ReadAllBytesAsync(DeployedPath, Ct));

            var timestamp = File.GetLastWriteTimeUtc(PackPath);
            AssertSucceeded(await Build(project));
            Assert.AreEqual(timestamp, File.GetLastWriteTimeUtc(PackPath));

            File.Delete(DeployedPath);
            AssertSucceeded(await Build(project));
            Assert.AreEqual(timestamp, File.GetLastWriteTimeUtc(PackPath));
            CollectionAssert.AreEqual(initialBytes, await File.ReadAllBytesAsync(DeployedPath, Ct));

            await File.WriteAllTextAsync(asset, "changed payload", Ct);
            File.SetLastWriteTimeUtc(PackPath, DateTime.UtcNow.AddMinutes(-5));
            AssertSucceeded(await Build(project));
            CollectionAssert.AreNotEqual(initialBytes, await File.ReadAllBytesAsync(PackPath, Ct));
            CollectionAssert.AreEqual(await File.ReadAllBytesAsync(PackPath, Ct),
                await File.ReadAllBytesAsync(DeployedPath, Ct));

            AssertSucceeded(await Build(project, "ComputeFilesToPublish"));
            Assert.AreEqual(PackName, (await File.ReadAllTextAsync(
                Path.Combine(TempDirectory, "publish.txt"), Ct)).Trim());

            File.Delete(QrcPath);
            AssertSucceeded(await Build(project));
            Assert.IsFalse(File.Exists(PackPath));
            Assert.IsFalse(File.Exists(DeployedPath));
        }

        [TestMethod]
        public async Task NoResourcesDoesNotRequireRcc()
        {
            var project = await WriteProjectAsync(Path.Combine(TempDirectory, "missing-rcc"));
            AssertSucceeded(await Build(project));
            Assert.IsFalse(File.Exists(PackPath));
        }

        [TestMethod]
        public async Task ResourcesReportMissingCompiler()
        {
            var project = await WriteProjectAsync(Path.Combine(TempDirectory, "missing-rcc"));
            await WriteQrcAsync();
            var result = await Build(project);
            Assert.AreNotEqual(0, result.Code);
            Assert.Contains("Qt resource compiler not found", result.Output);
        }

        [TestMethod]
        public async Task MissingResourceFailsBuild()
        {
            var project = await WriteProjectAsync(FindRcc());
            var asset = await WriteQrcAsync();
            AssertSucceeded(await Build(project));
            File.Delete(asset);
            Assert.AreNotEqual(0, (await Build(project)).Code);
        }

        [TestMethod]
        public async Task CleanRemovesPack()
        {
            var project = await WriteProjectAsync(FindRcc());
            await WriteQrcAsync();
            AssertSucceeded(await Build(project));
            AssertSucceeded(await Build(project, "QtBridgeClean"));
            Assert.IsFalse(File.Exists(PackPath));
            Assert.IsFalse(File.Exists(DeployedPath));
        }

        [TestMethod]
        public async Task ResourcePackNameIsConfigurable()
        {
            const string packName = "application-resources.rcc";
            var properties = new[] { "/p:QtBridgeResourcePackFileName=" + packName };
            var project = await WriteProjectAsync(FindRcc());
            await WriteQrcAsync();

            AssertSucceeded(await Build(project, properties: properties));
            var packPath = Path.Combine(Native, "build", packName);
            var deployedPath = Path.Combine(Output, packName);
            Assert.IsTrue(File.Exists(packPath));
            Assert.IsTrue(File.Exists(deployedPath));

            AssertSucceeded(await Build(project, "ComputeFilesToPublish", properties));
            Assert.AreEqual(packName,
                (await File.ReadAllTextAsync(Path.Combine(TempDirectory, "publish.txt"), Ct)).Trim());

            AssertSucceeded(await Build(project, "QtBridgeClean", properties));
            Assert.IsFalse(File.Exists(packPath));
            Assert.IsFalse(File.Exists(deployedPath));
        }

        [TestMethod]
        public async Task ResourcePackSupportsAnAbsoluteNativeBuildDirectory()
        {
            var redirectedBuildDirectory = Path.Combine(
                TempDirectory, "redirected", "native-build");
            var project = await WriteProjectAsync(FindRcc(), redirectedBuildDirectory);
            await WriteQrcAsync();

            AssertSucceeded(await Build(project));

            var packPath = Path.Combine(redirectedBuildDirectory, PackName);
            Assert.IsTrue(File.Exists(packPath));
            Assert.IsTrue(File.Exists(DeployedPath));
            Assert.IsFalse(File.Exists(PackPath));

            AssertSucceeded(await Build(project, "QtBridgeClean"));
            Assert.IsFalse(File.Exists(packPath));
            Assert.IsFalse(File.Exists(DeployedPath));
        }

        private async Task<string> WriteQrcAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(QrcPath)!);
            var asset = Path.Combine(TempDirectory, "a & b.txt");
            await File.WriteAllTextAsync(asset, "first payload", Ct);
            new XDocument(new XElement("RCC", new XElement("qresource",
                new XAttribute("prefix", "/"), new XElement("file",
                    new XAttribute("alias", "assemblies/App/asset.txt"), asset)))).Save(QrcPath);
            return asset;
        }

        private async Task<string> WriteProjectAsync(
            string rcc,
            string nativeBuildDirectory = "native/build")
        {
            Directory.CreateDirectory(TempDirectory);
            var targets = Path.Combine(FindRepositoryRoot(), "build", "Qt.Bridge.targets");
            var project = Path.Combine(TempDirectory, "Resources.proj");
            await File.WriteAllTextAsync(project, $"""
                <Project>
                  <PropertyGroup>
                    <ProjectDir>$(MSBuildProjectDirectory)/</ProjectDir>
                    <IntermediateOutputPath>obj/</IntermediateOutputPath>
                    <ProjectQtNativeRootDir>$(MSBuildProjectDirectory)/native</ProjectQtNativeRootDir>
                    <QtNativeBuildDir>{XmlEscape(nativeBuildDirectory)}</QtNativeBuildDir>
                    <TargetDir>$(MSBuildProjectDirectory)/output/</TargetDir>
                    <QtBridgeResourcePackFileName Condition="'$(QtBridgeResourcePackFileName)' == ''"
                      >{PackName}</QtBridgeResourcePackFileName>
                    <QtRccExecutable>{XmlEscape(rcc)}</QtRccExecutable>
                  </PropertyGroup>
                  <Import Project="{XmlEscape(targets)}" />
                  <Target Name="QtBridgeGenerate" />
                  <Target Name="ComputeFilesToPublish" />
                  <Target Name="DumpPublish" AfterTargets="QtBridgePublishResourcePack">
                    <WriteLinesToFile File="$(ProjectDir)publish.txt" Overwrite="true"
                      Lines="@(ResolvedFileToPublish->'%(RelativePath)')" />
                  </Target>
                </Project>
                """, Ct);
            return project;
        }

        private static string FindRcc()
        {
            var qtDir = Environment.GetEnvironmentVariable("QtDir");
            var rcc = Environment.GetEnvironmentVariable("QtRccExecutable");
            if (string.IsNullOrEmpty(rcc) && !string.IsNullOrEmpty(qtDir)) {
                // Same order as QtBridgeResolveResourcePack: distribution packages keep the
                // Qt 6 rcc under <libdir>/qt6, and bin/rcc may be the Qt 5 one there.
                var multiarch = RuntimeInformation.OSArchitecture == Architecture.Arm64
                    ? "aarch64-linux-gnu"
                    : "x86_64-linux-gnu";
                rcc = new[]
                    {
                        "bin/rcc.exe", "libexec/rcc", "lib64/qt6/libexec/rcc",
                        $"lib/{multiarch}/qt6/libexec/rcc", "lib/qt6/libexec/rcc", "lib/qt6/rcc",
                        "bin/rcc"
                    }
                    .Select(path => Path.Combine(qtDir, path)).FirstOrDefault(File.Exists);
            }
            if (string.IsNullOrEmpty(rcc) || !File.Exists(rcc))
                Assert.Inconclusive("Set QtDir or QtRccExecutable to run binary resource tests.");
            return rcc;
        }

        private static async Task<(int Code, string Output)> Build(string project,
            string target = "QtBridgeGenerate", params string[] properties)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(project)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            var arguments = new[] { "msbuild", project, "/nologo", "/v:minimal", "/t:" + target }
                .Concat(properties);
            foreach (var argument in arguments)
                info.ArgumentList.Add(argument);
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try {
                await process.WaitForExitAsync(timeout.Token);
            } catch (OperationCanceledException) {
                process.Kill(entireProcessTree: true);
                throw;
            }
            return (process.ExitCode, await output + await error);
        }

        private static void AssertSucceeded((int Code, string Output) result)
            => Assert.AreEqual(0, result.Code, result.Output);
    }
}
