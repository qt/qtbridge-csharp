// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.QtResources
{
    [TestClass]
    public class Test_QtResources : ManagedTestBase
    {
        [TestMethod]
        [DataRow("Linked", "SourceCode")]
        [DataRow("Linked", "Metadata")]
        [DataRow("Linked", "Mixed")]
        [DataRow("External", "SourceCode")]
        public async Task ResourcePipeline(string packaging, string exportMode)
        {
            using var temp = new TempProject();
            temp.ProjectRootDir = Path.Combine(temp.ProjectRootDir, packaging, exportMode);

            var options = new CreationOptions
            {
                Filename = "QtResources",
                PackageReferences = [Packages.QtBridge],
                ReplaceGeneratedFiles =
                [
                    (@"source\cpp\main.cpp", Path.Combine("QtResources", "main.cpp")),
                    (@"source\hpp\QtTestSetupBase.h", Path.Combine("Shared", "QtTestSetupBase.h")),
                ],
                AfterSdkTargets =
                    $$"""
                      <PropertyGroup>
                        <QtResourcePackaging>{{packaging}}</QtResourcePackaging>
                        <QtExportAs>metadata</QtExportAs>
                        <QtDefaultIcon>icon.png</QtDefaultIcon>
                      </PropertyGroup>
                      <ItemGroup>
                        <QtResource Include="sample.txt" />
                        <QtResource Include="icon.png"><Alias>icons/application.png</Alias></QtResource>
                      </ItemGroup>
                    """
                    + CMake.InjectQtSourcesTargets("hpp/QtTestSetupBase.h")
            };

            await InitializeAndBuildAsync(temp, options, project =>
            {
                project.AddFile("sample.txt", "Hello from Qt Resources!\n");
                File.WriteAllBytes(Path.Combine(project.ProjectDir, "icon.png"),
                    Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwC"
                        + "AAAAC0lEQVR42mP8/x8AAusB9Wl6ZbIAAAAASUVORK5CYII="));
                project.CopyFile("Program.cs", Path.Combine("QtResources", "Program.cs"));
                var firstExport = exportMode == "Mixed" ? "SourceCode" : exportMode;
                var secondExport = exportMode == "Mixed" ? "Metadata" : exportMode;
                project.AddFile("ExportedTypes.cs", $$"""
                    namespace QtResources {
                        [Qt.Export(Options = Qt.ExportAs.{{firstExport}})]
                        public class First { public int Value { get; set; } }
                        [Qt.Export(Options = Qt.ExportAs.{{secondExport}})]
                        public class Second { public int Value { get; set; } }
                    }
                    """);
            });

            var run = await temp.RunAsync(new RunOptions {
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut
            });

            AssertQTestExitCode(run.ExitCode, run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::resourceExists()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::resourceContent()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::iconAliases()", run.StdOut);
            Assert.Contains("PASS   : Test_QtResources::cleanupTestCase()", run.StdOut);
        }
    }
}
