// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public sealed class Test_DefaultQtNativeDirectories
    {
        private const string ProgramCs =
            """
                using System;

                namespace Test_DefaultQtNativeDirectories;

                internal static class Program
                {
                    private static int Main(string[] args)
                    {
                        Console.WriteLine("Default Qt native directories");
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

        [TestMethod]
        public async Task StayUnderIntermediateOutputPath()
        {
            using var temp = CreateProject();

            var buildOptions = new BuildOptions
            {
                Targets = [
                    "FindReferenceAssembliesForReferences",
                    "CoreCompile",
                    "QtBridgeGenerate"
                ],
                TargetPath = "",
                TargetExePath = ""
            };
            var (ok, output) = await temp.BuildAsync(buildOptions);

            temp.SaveLog("default-native-dirs");
            Assert.IsTrue(ok, output);

            var intermediateOutputPath = await temp.GetPropertyAsync("IntermediateOutputPath",
                buildOptions);
            var nativeSourceDir = await temp.GetPropertyAsync("QtNativeSourceDir", buildOptions);
            var nativeBuildDir = await temp.GetPropertyAsync("QtNativeBuildDir", buildOptions);
            var nativeBinDir = await temp.GetPropertyAsync("QtNativeBinDir", buildOptions);

            Assert.AreEqual(
                Normalize($"{intermediateOutputPath}qt/native/source"),
                Normalize(nativeSourceDir!));
            Assert.AreEqual(
                Normalize($"{intermediateOutputPath}qt/native/build"),
                Normalize(nativeBuildDir!));
            Assert.AreEqual(
                Normalize($"{intermediateOutputPath}qt/native/bin"),
                Normalize(nativeBinDir!));

            var expectedSourceDirectory = Path.Combine(temp.ProjectDir, nativeSourceDir!);
            Assert.IsTrue(File.Exists(Path.Combine(expectedSourceDirectory, "CMakeLists.txt")));
            Assert.IsTrue(File.Exists(Path.Combine(
                expectedSourceDirectory,
                "qml",
                "Application",
                "Main.qml")));
            Assert.IsFalse(Directory.Exists(Path.Combine(temp.ProjectDir, "qt")));
        }

        [TestMethod]
        public async Task UseForwardSlashesInDefaultDirectoryProperties()
        {
            using var temp = CreateProject();

            var buildOptions = new BuildOptions
            {
                Config = Config.Release,
                Targets = [
                    "FindReferenceAssembliesForReferences",
                    "CoreCompile",
                    "QtBridgeGenerate"
                ],
                Properties = [("Platform", "x64")],
                TargetPath = "",
                TargetExePath = ""
            };

            var (ok, output) = await temp.BuildAsync(buildOptions);

            temp.SaveLog("default-native-dirs-forward-slashes");
            Assert.IsTrue(ok, output);

            var nativeSourceDir = await temp.GetPropertyAsync("QtNativeSourceDir", buildOptions);
            var nativeBuildDir = await temp.GetPropertyAsync("QtNativeBuildDir", buildOptions);
            var nativeBinDir = await temp.GetPropertyAsync("QtNativeBinDir", buildOptions);

            Assert.DoesNotContain('\\', nativeSourceDir);
            Assert.DoesNotContain('\\', nativeBuildDir);
            Assert.DoesNotContain('\\', nativeBinDir);
            Assert.EndsWith("/qt/native/source", nativeSourceDir);
            Assert.EndsWith("/qt/native/build", nativeBuildDir);
            Assert.EndsWith("/qt/native/bin", nativeBinDir);
        }

        [TestMethod]
        public async Task FollowCustomizedIntermediateOutputLayout()
        {
            using var temp = CreateProject(
                beforeSdkProps:
                """
                   <PropertyGroup>
                     <BaseOutputPath>artifacts/bin/</BaseOutputPath>
                     <BaseIntermediateOutputPath>artifacts/obj/</BaseIntermediateOutputPath>
                   </PropertyGroup>
                """);

            var buildOptions = new BuildOptions
            {
                Config = Config.Release,
                Targets = [
                    "FindReferenceAssembliesForReferences",
                    "CoreCompile",
                    "QtBridgeGenerate"
                ],
                Properties = [("Platform", "x64")],
                TargetPath = "",
                TargetExePath = ""
            };

            var (ok, output) = await temp.BuildAsync(buildOptions);

            temp.SaveLog("default-native-dirs-custom-intermediate");
            Assert.IsTrue(ok, output);

            var intermediateOutputPath = await temp.GetPropertyAsync("IntermediateOutputPath",
                buildOptions);
            var nativeSourceDir = await temp.GetPropertyAsync("QtNativeSourceDir", buildOptions);
            var nativeBuildDir = await temp.GetPropertyAsync("QtNativeBuildDir", buildOptions);
            var nativeBinDir = await temp.GetPropertyAsync("QtNativeBinDir", buildOptions);

            Assert.StartsWith("artifacts/obj/", intermediateOutputPath);
            Assert.AreEqual(
                Normalize($"{intermediateOutputPath}qt/native/source"),
                Normalize(nativeSourceDir!));
            Assert.AreEqual(
                Normalize($"{intermediateOutputPath}qt/native/build"),
                Normalize(nativeBuildDir!));
            Assert.AreEqual(
                Normalize($"{intermediateOutputPath}qt/native/bin"),
                Normalize(nativeBinDir!));
            Assert.IsFalse(Directory.Exists(Path.Combine(temp.ProjectDir, "qt")));
        }

        private static TempProject CreateProject(string beforeSdkProps = "")
        {
            var temp = new TempProject();
            temp.Create(new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                BeforeSdkProps = beforeSdkProps
            });
            temp.AddFile("Program.cs", ProgramCs);
            temp.AddFile("Main.qml", MainQml);
            return temp;
        }

        private static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');
    }
}
