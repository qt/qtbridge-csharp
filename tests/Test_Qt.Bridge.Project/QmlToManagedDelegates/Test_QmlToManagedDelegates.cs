// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.QmlToManagedDelegates
{
    [TestClass]
    public class Test_QmlToManagedDelegates : ManagedTestBase
    {
        [TestMethod]
        public async Task QmlToManagedDelegates()
        {
            using var temp = new TempProject();

            var options = CreateQtQuickTestOptions(Path.Combine("QmlToManagedDelegates", "main.cpp"));
            await InitializeAndBuildAsync(temp, options,
                project => {
                    project.CopyFile("Program.cs",
                        Path.Combine("QmlToManagedDelegates", "Program.cs"));
                    project.CopyFile("tst_qmltomanageddelegates.qml", Path
                        .Combine("QmlToManagedDelegates", "tst_qmltomanageddelegates.qml"));
                });

            var run = await temp.RunAsync(new() {
                Args = [
                    "-input",
                    Path.Combine(temp.ExeDir, "Application", "tst_qmltomanageddelegates.qml")
                ],
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            const string pass = "PASS   : Test_QmlToManagedDelegates::tst_qmltomanageddelegates::";

            Assert.Contains(pass + "initTestCase()", run.StdOut);
            Assert.Contains(pass + "test_delegate_param_callback_int_return()", run.StdOut);
            Assert.Contains(pass + "test_void_delegate()", run.StdOut);
            Assert.Contains(pass + "test_multiple_invocations()", run.StdOut);
            Assert.Contains(pass + "test_multi_param_delegate()", run.StdOut);
            Assert.Contains(pass + "test_no_param_delegate()", run.StdOut);
            Assert.Contains(pass + "test_js_exception_propagates()", run.StdOut);
            Assert.Contains(pass + "test_object_as_delegate_arg()", run.StdOut);
            Assert.Contains(pass + "test_object_as_delegate_return()", run.StdOut);
            Assert.Contains(pass + "test_null_delegate()", run.StdOut);
            Assert.Contains(pass + "test_bcl_action_delegate()", run.StdOut);
            Assert.Contains(pass + "test_bcl_func_delegate()", run.StdOut);
            Assert.Contains(pass + "test_delegate_property()", run.StdOut);
            Assert.Contains(pass + "test_managed_delegate_property_invokers()", run.StdOut);
            Assert.Contains(pass + "test_null_managed_delegate_property_invoker()", run.StdOut);
            Assert.Contains(pass + "cleanupTestCase()", run.StdOut);
        }
    }
}
