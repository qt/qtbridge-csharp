// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Test_Qt.Bridge.Project.BugFix
{
    using Shared;

    [TestClass]
    public class Test_InboundMemLeak : ManagedTestBase
    {
        private static TempProject sharedProject;

        private static IEnumerable<Type> Types =>
        [
            typeof(void),
            typeof(int),
            typeof(char),
            typeof(string),
            typeof(DateTime),
            typeof(Uri),
            typeof(object)
        ];

        private static IEnumerable<int> Loops => [200000];

        public static IEnumerable<(string TypeName, int LoopCount)> TestCases =>
            Types.SelectMany(t => Loops.Select(n => (t.Name, n)));

        [ClassInitialize]
        public static async Task BuildTestProject(TestContext _)
        {
            if (!AssemblyMetadata.Settings.RunInboundMemoryLeak)
                return;

            // Compile all inbound signatures once. Each data row still launches a separate
            // process, keeping its managed heap and memory samples isolated from the others.
            sharedProject = new TempProject();

            var options = CreateQtTestOptions(Path.Combine(sharedProject.ProjectDir, "main.cpp"));
            options.Reset = false;
            options.PackageReferences.Add(("MathNet.Numerics", "5.0.0"));
            await InitializeAndBuildAsync(sharedProject, options, proj =>
            {
                proj.CopyFile("Program.cs", Path.Combine("BugFix", "QTBRIDGES-330", "Program.cs"));
                proj.CopyFile("main.cpp", Path.Combine("BugFix", "QTBRIDGES-330", "main.cpp"));
                File.WriteAllLines(Path.Combine(sharedProject.ProjectDir, "main.cpp"),
                [
                    $"#define TYPE_NAME \"Test_InboundMemLeak.Functions, {
                        sharedProject.ProjectFilename}\"",
                    string.Empty,
                    File.ReadAllText(Path.Combine(sharedProject.ProjectDir, "main.cpp"))
                ]);
            });
        }

        [ClassCleanup]
        public static void CleanupTestProject()
        {
            sharedProject?.Dispose();
            sharedProject = null;
        }

        [TestMethod, DynamicData(nameof(TestCases))]
        [WorkItem(330), Description("https://qt-project.atlassian.net/browse/QTBRIDGES-330")]
        public Task Inbound_DoesNotLeak(string typeName, int loopCount)
            => RunLoopAsync($"loopInbound{typeName}", loopCount);

        // Strings that native code passes to the adapter itself, such as type and method names.
        [TestMethod]
        [WorkItem(330), Description("https://qt-project.atlassian.net/browse/QTBRIDGES-330")]
        public Task AdapterStrings_DoNotLeak()
            => RunLoopAsync("loopAdapterStrings", Loops.First());

        private static async Task RunLoopAsync(string testFunction, int loopCount)
        {
            if (!AssemblyMetadata.Settings.RunInboundMemoryLeak) {
                Assert.Inconclusive("Skipping inbound memory-leak test because "
                    + "TestRunInboundMemoryLeak is false.");
            }

            var run = await sharedProject.RunAsync(new()
            {
                Args = [testFunction],
                EnvVars =
                [
                    ("QT_FORCE_STDERR_LOGGING", "1"),
                    ("QTBRIDGE_INBOUND_ITERATIONS", $"{loopCount}")
                ],
                StdErr = Redirect.StdOut,
                Timeout = -1
            });

            AssertQTestExitCode(run.ExitCode);

            if (run.StdOut.Contains(
                $"Test_InboundMemLeak::{testFunction}() Test function timed out"))
                Assert.Inconclusive("Benchmark loop timeout");

            Assert.Contains("PASS   : Test_InboundMemLeak::initTestCase()", run.StdOut);
            Assert.Contains($"PASS   : Test_InboundMemLeak::{testFunction}()", run.StdOut);
            Assert.Contains("PASS   : Test_InboundMemLeak::cleanupTestCase()", run.StdOut);

            Console.WriteLine(run.StdOut);
        }
    }
}
