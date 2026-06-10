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
            Convert = 1 << 1,
            Dispatch = 1 << 2,
            Main = 1 << 3,
            MyClassA = 1 << 4,
            MyClassB = 1 << 5
        }

        [TestMethod
            , DataRow("CleanBuild", null
                , Cpp.Cast | Cpp.Convert | Cpp.Dispatch | Cpp.Main | Cpp.MyClassA)
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

            check = cppFiles.HasFlag(Cpp.Convert) ? Assert.IsTrue : Assert.IsFalse;
            check(target.HasMessage(new(@"\bconvert.cpp\b")));

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
    }
}
