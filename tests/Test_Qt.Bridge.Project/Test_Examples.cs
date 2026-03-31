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
            if (Environment.GetEnvironmentVariable("SKIP_EXAMPLES_TEST") is { } value
                && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)))
            {
                Assert.Inconclusive("Skipping examples build because SKIP_EXAMPLES_TEST is set.");
            }

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
            await msbuild.WaitForExitAsync(TestContext.CancellationTokenSource.Token);

            var buildSummary = Regex
                .Match(buildMsgs.ToString(), @"(?<=\n)Build (FAILED|succeeded)\.(?:.|[\r\n])*");
            if (buildSummary.Success && buildSummary.Value is { Length: > 0 })
                TestContext?.WriteLine(buildSummary.Value);

            Assert.AreEqual(0, msbuild.ExitCode);
        }
    }
}
