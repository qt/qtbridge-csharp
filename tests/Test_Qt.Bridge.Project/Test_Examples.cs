// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public class Test_Examples
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Build_Examples()
        {
            if (AssemblyMetadata.Build.ProjectDir is not { Length: > 0 })
                Assert.Inconclusive();
            var examplesDir = Path.GetFullPath(Path
                .Combine(AssemblyMetadata.Build.ProjectDir, "..", "..", "examples"));
            if (!Directory.Exists(examplesDir))
                Assert.Inconclusive();

            StringBuilder buildMsgs = new();
            Action<string> log = x => buildMsgs.AppendLine(x);
            var msbuild = MsBuild.Start(log, log, examplesDir, [],
                "-restore", "-p:Configuration=Debug", "-p:Platform=Any CPU", "-m", "-t:Rebuild");
            await msbuild.WaitForExitAsync();

            var buildSummary = Regex
                .Match(buildMsgs.ToString(), @"(?<=\n)Build (FAILED|succeeded)\.(?:.|[\r\n])*");
            if (buildSummary.Success && buildSummary.Value is { Length: > 0 })
                TestContext?.WriteLine(buildSummary.Value);

            Assert.AreEqual(msbuild.ExitCode, 0);
        }
    }
}
