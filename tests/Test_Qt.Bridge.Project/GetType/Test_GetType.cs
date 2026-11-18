// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.GetType
{
    [TestClass]
    public class Test_GetType : ManagedTestBase
    {
        [TestMethod]
        [DataRow("public int Value { get; set; }", DisplayName = "GetType build: control")]
        [DataRow("public int Type { get; set; }", DisplayName = "GetType build: Type property")]
        [DataRow("public int Type() => 123;", DisplayName = "GetType build: Type method")]
        [DataRow("public int getType() => 123;", DisplayName = "GetType build: getType method")]
        [DataRow("public new string GetType { get; set; }",
            DisplayName = "GetType build: GetType property")]
        public async Task GetType_With_Exported_Member_Builds(string member)
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine("GetType", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", Path.Combine("GetType", "Program.cs"));
                    var programPath = Path.Combine(project.ProjectDir, "Program.cs");
                    var program = File.ReadAllText(programPath);
                    const string declaration = "public class Bar { }";
                    Assert.Contains(declaration, program);
                    File.WriteAllText(programPath,
                        program.Replace(declaration, $"public class Bar {{ {member} }}"));
                    project.CopyFile("tst_gettype.qml", Path.Combine("GetType", "tst_gettype.qml"));
                });
        }

        [TestMethod]
        public async Task GetType_Returns_Type_ToString()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine("GetType", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs", Path.Combine("GetType", "Program.cs"));
                    project.CopyFile("tst_gettype.qml", Path
                        .Combine("GetType", "tst_gettype.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = ["-input", Path.Combine(temp.ExeDir, "Application", "tst_gettype.qml")],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            AssertQTestExitCode(run.ExitCode);

            var pass = "PASS   : Test_GetType::tst_gettype::";
            Assert.Contains(pass + "initTestCase()", run.StdOut);
            Assert.Contains(pass + "test_getType_metadata()", run.StdOut);
            Assert.Contains(pass + "test_getType_source()", run.StdOut);
            Assert.Contains(pass + "test_discard_return_value()", run.StdOut);
            Assert.Contains(pass + "test_helper_has_priority()", run.StdOut);
            Assert.Contains(pass + "cleanupTestCase()", run.StdOut);
        }
    }
}
