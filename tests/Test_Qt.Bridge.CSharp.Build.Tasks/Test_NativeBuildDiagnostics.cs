// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_NativeBuildDiagnostics : TestBase
    {
        protected override string TempDirectoryName => "qtbridge-native-diagnostics-tests";

        private const string Reason = "- type 'Sample.Editor' is exported as source code";

        [TestMethod]
        public async Task MissingCMakeNamesTheReasonAndTheExecutable()
        {
            var project = WriteProject(Path.Combine(TempDirectory, "no-such-cmake"), reason: Reason);

            var result = await BuildAsync(project);

            Assert.AreNotEqual(0, result.Code);
            Assert.Contains("CMake could not be run from", result.Output);
            Assert.Contains("Native compilation is required because:", result.Output);
            Assert.Contains(Reason, result.Output);
            Assert.DoesNotContain("Native configuration failed", result.Output);
        }

        [TestMethod]
        public async Task NativeHostTemplateNamesItselfAndDoesNotSuggestMetadata()
        {
            var project = WriteProject(Path.Combine(TempDirectory, "no-such-cmake"),
                reason: Reason, hostTemplate: true);

            var result = await BuildAsync(project);

            Assert.AreNotEqual(0, result.Code);
            Assert.Contains("CMake could not be run from", result.Output);
            Assert.Contains("- this project builds the native host template", result.Output);
            Assert.DoesNotContain("Sample.Editor", result.Output);
            Assert.DoesNotContain("QtExportAs", result.Output);
        }

        [TestMethod]
        public async Task StaleStatusWithoutReasonFileNeverShowsAnEmptyReason()
        {
            var project = WriteProject(Path.Combine(TempDirectory, "no-such-cmake"), reason: null);

            var result = await BuildAsync(project);

            Assert.AreNotEqual(0, result.Code);
            Assert.Contains("- generated native build state is present", result.Output);
        }

        [TestMethod]
        public async Task ConfigureFailureKeepsCMakeOutputAndAddsTheReason()
        {
            var project = WriteProject(WriteFakeCMake(), reason: Reason);

            var result = await BuildAsync(project);

            Assert.AreNotEqual(0, result.Code);
            Assert.Contains("fake cmake configure output", result.Output);
            Assert.Contains("Native configuration failed", result.Output);
            Assert.Contains(Reason, result.Output);
            Assert.DoesNotContain("CMake could not be run", result.Output);
        }

        [TestMethod]
        public async Task ChangedQtDirResetsTheCMakeCache()
        {
            var project = WriteProject(WriteFakeCMake(), reason: Reason);
            var buildDir = WriteCMakeCache(Path.Combine(TempDirectory, "old-qt"));

            var result = await BuildAsync(project);

            Assert.Contains("Qt installation changed, resetting the CMake cache.", result.Output);
            Assert.IsFalse(File.Exists(Path.Combine(buildDir, "CMakeCache.txt")));
            Assert.IsFalse(Directory.Exists(Path.Combine(buildDir, "CMakeFiles")));
        }

        [TestMethod]
        public async Task UnchangedQtDirKeepsTheCMakeCache()
        {
            var project = WriteProject(WriteFakeCMake(), reason: Reason);
            var buildDir = WriteCMakeCache(Path.Combine(TempDirectory, "qt"));

            var result = await BuildAsync(project);

            Assert.DoesNotContain("Qt installation changed", result.Output);
            Assert.IsTrue(File.Exists(Path.Combine(buildDir, "CMakeCache.txt")));
        }

        // Simulates a previous configure against the Qt in qtDir.
        private string WriteCMakeCache(string qtDir)
        {
            var buildDir = Path.Combine(TempDirectory, "native", "build");
            Directory.CreateDirectory(Path.Combine(buildDir, "CMakeFiles"));
            File.WriteAllText(Path.Combine(buildDir, "CMakeCache.txt"), "");
            File.WriteAllText(Path.Combine(buildDir, "qt_bridge_qtdir.txt"),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(qtDir))
                + Path.DirectorySeparatorChar);
            return buildDir;
        }

        // Succeeds for --version, so the probe passes, and fails for anything else.
        private string WriteFakeCMake()
        {
            Directory.CreateDirectory(TempDirectory);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                var cmd = Path.Combine(TempDirectory, "fake-cmake.cmd");
                File.WriteAllText(cmd, "@echo off\r\nif \"%1\"==\"--version\" exit /b 0\r\n"
                    + "echo fake cmake configure output\r\nexit /b 1\r\n");
                return cmd;
            }

            var sh = Path.Combine(TempDirectory, "fake-cmake.sh");
            File.WriteAllText(sh, "#!/bin/sh\n[ \"$1\" = \"--version\" ] && exit 0\n"
                + "echo fake cmake configure output\nexit 1\n");
            File.SetUnixFileMode(sh, UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute);
            return sh;
        }

        private string WriteProject(string cmake, string? reason, bool hostTemplate = false)
        {
            Directory.CreateDirectory(TempDirectory);
            var native = Path.Combine(TempDirectory, "native");
            Directory.CreateDirectory(native);
            File.WriteAllText(Path.Combine(native, "source_code_status.txt"), "Sample.Editor\n");
            if (reason != null)
                File.WriteAllText(Path.Combine(native, "native_build_reason.txt"), reason + "\n");

            var qtDir = Path.Combine(TempDirectory, "qt");
            Directory.CreateDirectory(Path.Combine(qtDir, "lib", "cmake", "Qt6"));
            File.WriteAllText(Path.Combine(qtDir, "lib", "cmake", "Qt6", "Qt6Config.cmake"), "");

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null) {
                if (File.Exists(Path.Combine(directory.FullName, "build", "Qt.Bridge.targets")))
                    break;
                directory = directory.Parent;
            }

            Assert.IsNotNull(directory);
            var targets = Path.Combine(directory.FullName, "build", "Qt.Bridge.targets");

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows).ToString();
            var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX).ToString();
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux).ToString();
            var project = Path.Combine(TempDirectory, "Native.proj");
            var isNativeHost = hostTemplate.ToString().ToLowerInvariant();
            File.WriteAllText(project, $"""
                <Project>
                  <PropertyGroup>
                    <ProjectDir>$(MSBuildProjectDirectory)/</ProjectDir>
                    <IntermediateOutputPath>obj/</IntermediateOutputPath>
                    <ProjectQtNativeRootDir>$(MSBuildProjectDirectory)/native</ProjectQtNativeRootDir>
                    <QtNativeBuildDir>native/build</QtNativeBuildDir>
                    <QtNativeBinDir>native/bin</QtNativeBinDir>
                    <QtGenSourceCode>true</QtGenSourceCode>
                    <QtBridgeIsNativeHostTemplate>{isNativeHost}</QtBridgeIsNativeHostTemplate>
                    <QtDir>{SecurityElement.Escape(qtDir)}</QtDir>
                    <QtCMakeExecutable>{SecurityElement.Escape(cmake)}</QtCMakeExecutable>
                    <QtHostIsWindows>{isWindows.ToLowerInvariant()}</QtHostIsWindows>
                    <QtHostIsMacOS>{isMacOs.ToLowerInvariant()}</QtHostIsMacOS>
                    <QtHostIsLinux>{isLinux.ToLowerInvariant()}</QtHostIsLinux>
                  </PropertyGroup>
                  <Import Project="{SecurityElement.Escape(targets)}" />
                  <Target Name="QtBridgeGenerate" />
                </Project>
                """);
            return project;
        }

        private static async Task<(int Code, string Output)> BuildAsync(string project)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(project)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            foreach (var argument in new[] { "msbuild", project, "/nologo", "/v:minimal",
                "/t:QtBridgeBuild" }) {
                info.ArgumentList.Add(argument);
            }

            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try {
                await process.WaitForExitAsync(timeout.Token);
            } catch (OperationCanceledException) {
                process.Kill(entireProcessTree: true);
                throw;
            }
            return (process.ExitCode, await output + await error);
        }
    }
}
