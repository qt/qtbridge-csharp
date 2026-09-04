// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public class Test_IncrementalBuild
    {
        private const string ProgramCs = $@"
using Qt.Quick;
namespace Test_IncrementalBuild
{{
    //REMOVE_CLASS// /*
    public class MyClassA
    {{
        //PRIVATE_FUNC// private int MyPrivateFunc() => 42;
        //PUBLIC_FUNC// public int MyPublicFunc() => 42;
    }}
    //REMOVE_CLASS// */
    //PRIVATE_CLASS// internal class MyOtherClass {{ }}
    //PUBLIC_CLASS// public class MyClassB {{ }}

    internal class Program {{ static int Main(string[] args) => 0; }}
}}
";
        private const string MainQml = $@"
import QtQuick
ApplicationWindow {{
    id: window; width: 220; height: 240; visible: true; title: ""Test_IncrementalBuild""
}}
";
        [Flags]
        public enum Cpp
        {
            None = 0,
            Cast = 1 << 0,
            Dispatch = 1 << 2,
            Main = 1 << 3,
            MyClassA = 1 << 4,
            MyClassB = 1 << 5
        }

        [TestMethod
            , DataRow("CleanBuild", null
                , Cpp.Cast | Cpp.Dispatch | Cpp.Main | Cpp.MyClassA)
            , DataRow("NoChanges", "", Cpp.None)
            , DataRow("QmlChanged", "QML", Cpp.None)
            , DataRow("PrivateFunc", "PRIVATE_FUNC", Cpp.None)
            , DataRow("PublicFunc", "PUBLIC_FUNC", Cpp.MyClassA)
            , DataRow("PrivateClass", "PRIVATE_CLASS", Cpp.None)
            , DataRow("PublicClass", "PUBLIC_CLASS", Cpp.Cast | Cpp.Dispatch | Cpp.MyClassB)
            , DataRow("RemoveClass", "REMOVE_CLASS", Cpp.Cast | Cpp.Dispatch)
        ]
        public async Task IncrementalBuild(string context, string action, Cpp cppFiles)
        {
            string[] targets = [
                "FindReferenceAssembliesForReferences",
                "CoreCompile",
                "QtBridgeGenerate"
            ];
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge]
            });
            temp.AddFile("Program.cs", ProgramCs);
            temp.AddFile("Main.qml", MainQml);
            await temp.BuildAsync(new() { Targets = targets });
            if (action != null) {
                switch (action) {
                    case "":
                        break;
                    case "QML":
                        temp.AddFile("Main.qml", MainQml);
                        break;
                    default:
                        temp.AddFile("Program.cs", ProgramCs.Replace($"//{action}//", ""));
                        break;
                }
                await temp.BuildAsync(new() { Targets = targets });
            }
            temp.SaveLog(context);
            Assert.IsTrue(temp.Log.TryFindTarget("QtBridgeGenerate", out var target));

            Action<bool> check = cppFiles.HasFlag(Cpp.Main) ? Assert.IsTrue : Assert.IsFalse;
            check(target.HasMessage(new(@"\bmain.cpp\b")));

            check = cppFiles.HasFlag(Cpp.Dispatch) ? Assert.IsTrue : Assert.IsFalse;
            check(target.HasMessage(new(@"\bobject_dispatch.cpp\b")));

            check = cppFiles.HasFlag(Cpp.Cast) ? Assert.IsTrue : Assert.IsFalse;
            check(target.HasMessage(new(@"\btypecast.cpp\b")));

            check = cppFiles.HasFlag(Cpp.MyClassA) ? Assert.IsTrue : Assert.IsFalse;
            check(target.HasMessage(new(@"\bmyclassa.cpp\b")));

            check = cppFiles.HasFlag(Cpp.MyClassB) ? Assert.IsTrue : Assert.IsFalse;
            check(target.HasMessage(new(@"\bmyclassb.cpp\b")));
        }

        // QtResources.cs is declared as an UpToDateCheckOutput (Set="QtResource") regardless of
        // whether the project has any @(QtResource)/@(QtResx) items. If it is only written when
        // such items are present, VS FastUpToDate will report the output as missing and force a
        // full rebuild on every build, even with no changes.
        [TestMethod]
        public async Task QtResourcesOutputWrittenWithoutResourceItems()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge]
            });
            temp.AddFile("Program.cs", ProgramCs);
            temp.AddFile("Main.qml", MainQml);
            await temp.BuildAsync(new() { Targets = ["CoreCompile"] });

            var outputPath = await temp.GetPropertyAsync("IntermediateOutputPath");
            var qtResourceFilesCs = await temp.GetPropertyAsync("QtResourceFilesCs");
            var qtResourcesCs = Path.Combine(temp.ProjectDir, outputPath, qtResourceFilesCs);
            Assert.IsTrue(File.Exists(qtResourcesCs), $"Expected '{qtResourcesCs}' to be written "
                + "always, since it is declared as an UpToDateCheckOutput.");
        }

        [TestMethod]
        public async Task QtResourceAliasUsesForwardSlashes()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge],
                AfterSdkTargets = """
                  <ItemGroup>
                    <QtResource Include="asset.txt">
                      <Alias>qml\MyModule\asset.txt</Alias>
                    </QtResource>
                  </ItemGroup>
                  """
            });
            temp.AddFile("Program.cs", ProgramCs);
            temp.AddFile("asset.txt", "resource");
            await temp.BuildAsync(new() { Targets = ["CoreCompile"] });

            var outputPath = await temp.GetPropertyAsync("IntermediateOutputPath");
            var qtResourceFilesCs = await temp.GetPropertyAsync("QtResourceFilesCs");
            var qtResourcesCs = Path.Combine(temp.ProjectDir, outputPath, qtResourceFilesCs);
            var contents = File.ReadAllText(qtResourcesCs);
            StringAssert.Contains(contents, "Alias = \"qml/MyModule/asset.txt\"");
        }

        [TestMethod]
        public async Task QtResourceLooseDeploymentUsesQmlAliasPath()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge],
                AfterSdkTargets = """
                  <PropertyGroup>
                    <TargetDir>$(MSBuildProjectDirectory)/loose/</TargetDir>
                  </PropertyGroup>
                  <ItemGroup>
                    <QtResource Include="assets/logo.svg">
                      <Alias>qt\qml\Application\images\logo.svg</Alias>
                    </QtResource>
                    <QtResource Include="internal.txt" />
                  </ItemGroup>
                  """
            });
            temp.AddFile("assets/logo.svg", "logo");
            temp.AddFile("internal.txt", "internal");

            var (buildOk, buildOutput) = await temp.BuildAsync(new()
            {
                Targets = ["QtBridgeAddResources", "QtBridgeDeployResources"],
                TargetPath = "",
                TargetExePath = ""
            });
            Assert.IsTrue(buildOk, buildOutput);

            var deployDir = Path.Combine(temp.ProjectDir, "loose");
            Assert.IsTrue(File.Exists(Path.Combine(deployDir, "Application", "images", "logo.svg")));
            Assert.IsFalse(File.Exists(Path.Combine(deployDir, "assets", "logo.svg")));
            Assert.IsFalse(File.Exists(Path.Combine(deployDir, "internal.txt")));
            CollectionAssert.AreEqual(
                new[] { "Application/images/logo.svg" },
                File.ReadAllLines(Path.Combine(deployDir, "qtdeploy.txt")));
        }
    }
}
