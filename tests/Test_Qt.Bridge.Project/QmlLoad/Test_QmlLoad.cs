// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace Test_Qt.Bridge.Project.QmlLoad
{
    [TestClass]
    public class Test_QmlLoad
    {
        private enum Result
        {
            Ok,
            Fail_Exception,
            Fail_AutoArgs,
            Fail_ManualArgs,
            Fail_NullRefArg,
            Fail_Exception_NullRefArg,
            Fail_DuplicateKeys,
            Fail_Exception_DuplicateKeys,
            Fail_HiddenProperty,
            Fail_Exception_HiddenProperty,
            Fail_SetterOnly,
            Fail_EmptyModule,
            Fail_AutoNullValue,
            Fail_ManualNullValue
        }

        [TestMethod]
        public async Task QmlLoad()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge],
                OutputType = OutputType.Exe
            });
            temp.CopyFile("Program.cs", Path.Combine("QmlLoad", "Program.cs"));
            temp.CopyFile("Main.qml", Path.Combine("QmlLoad", "Main.qml"));
            temp.AddFile("TestResult.cs", $$"""
                namespace Test_QmlLoad
                {
                    internal enum Result
                    {
                {{string.Join($",{Environment.NewLine}", Enum.GetValues<Result>()
                    .Select(res => $"        {res} = {(int)res}"))}}
                    }
                }
                """);

            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok, build.Output);

            var run = await temp.RunAsync(new() {
                EnvVars = [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QML_DISABLE_DISK_CACHE", "1")
                ],
                StdErr = Redirect.StdOut
            });
            Logger.LogMessage(run.StdOut);
            foreach (var errorResult in Enum.GetValues<Result>().Where(res => res != Result.Ok))
                Assert.AreNotEqual(errorResult, (Result)run.ExitCode);
            Assert.AreEqual(Result.Ok, (Result)run.ExitCode);
        }
    }
}
