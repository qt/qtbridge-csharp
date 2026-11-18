// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.Bridge.CodeGeneration;
using Qt.Bridge.Utils.Concurrent;

namespace Test_Qt.Bridge.CSharp.Generator
{
    [TestClass]
    public class Test_QtCmd
    {
        [TestMethod]
        [DataRow("lib/qt6/bin", false)]
        [DataRow("lib64/qt6/bin", false)]
        [DataRow("lib/x86_64-linux-gnu/qt6/bin", true)]
        [DataRow("lib/aarch64-linux-gnu/qt6/libexec", true)]
        public void FindsToolInDistributionLayout(string relativeDirectory, bool useLibDir)
        {
            var root = Path
                .Combine(Path.GetTempPath(), "qtbridge-tools-" + Guid.NewGuid().ToString("N"));
            var previousQtDir = Rules.QtDir;
            var previousLibDir = Rules.QtLibDir;
            try {
                var directory = Path.Combine(root, relativeDirectory);
                Directory.CreateDirectory(directory);
                var tool = Path
                    .Combine(directory, OperatingSystem.IsWindows() ? "qmldom.exe" : "qmldom");
                File.WriteAllText(tool, "tool lookup fixture");
                Rules.QtDir = new DirectoryInfo(root);
                Rules.QtLibDir = useLibDir
                    ? new DirectoryInfo(Path.GetFullPath(Path.Combine(directory, "../.."))) : null;

                Assert.AreEqual(Path.GetFullPath(tool), Rule.QtCmd.FindTool("qmldom"));
            } finally {
                Rules.QtDir = previousQtDir;
                Rules.QtLibDir = previousLibDir;
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public async Task TimeoutTerminatesTheStartedProcess()
        {
            using var process = CmdProc.Start(
                OperatingSystem.IsWindows() ? "ping.exe" : "sleep", Environment.CurrentDirectory,
                OperatingSystem.IsWindows() ? ["-n", "30", "127.0.0.1"] : ["30"],
                _ => { }, _ => { });
            try {
                await Assert.ThrowsExactlyAsync<TimeoutException>(() =>
                    Rule.QtCmd.WaitForExitAsync(process, TimeSpan.FromMilliseconds(200)));
                Assert.IsTrue(process.HasExited, "The timed-out tool must not remain running.");
            } finally {
                if (!process.HasExited) {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
        }
    }
}
