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

        [TestMethod, DynamicData(nameof(TestCases))]
        [WorkItem(330), Description("https://qt-project.atlassian.net/browse/QTBRIDGES-330")]
        public async Task Inbound_DoesNotLeak(string typeName, int loopCount)
        {
            using var temp = new TempProject();

            var options = CreateQtTestOptions(Path.Combine(temp.ProjectDir, "main.cpp"));
            options.Reset = false;
            options.PackageReferences.Add(("MathNet.Numerics", "5.0.0"));
            await InitializeAndBuildAsync(temp, options, proj =>
            {
                proj.CopyFile("Program.cs", Path.Combine("BugFix", "QTBRIDGES-330", "Program.cs"));
                proj.CopyFile("main.cpp", Path.Combine("BugFix", "QTBRIDGES-330", "main.cpp"));
                File.WriteAllLines(Path.Combine(temp.ProjectDir, "main.cpp"),
                [
                    $"#define TYPE_NAME \"Test_InboundMemLeak.Functions, {temp.ProjectFilename}\"",
                    $"#define INBOUND_TYPE {typeName switch {
                        "Void" => "void",
                        "Int32" => "qint32",
                        "Char" => "QChar",
                        "String" => "QString",
                        "DateTime" => "QDateTime",
                        "Uri" => "QUrl",
                        "Object" => "QDotNetRef",
                        _ => throw new AssertFailedException($"Unexpected test data: {typeName}")
                    }}",
                    $"#define INBOUND_FUNC \"Inbound{typeName}\"",
                    string.Empty,
                    File.ReadAllText(Path.Combine(temp.ProjectDir, "main.cpp"))
                ]);
            });

            var run = await temp.RunAsync(new()
            {
                Args = ["-iterations", $"{loopCount}"],
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut,
                Timeout = -1
            });

            Assert.IsLessThanOrEqualTo((int)ExitCode.QTestFailure, run.ExitCode,
                ExitCodeHelper.ToString(run.ExitCode));

            if (run.StdOut.Contains("Test_InboundMemLeak::loopInbound() Test function timed out"))
                Assert.Inconclusive("Benchmark loop timeout");

            Assert.Contains("PASS   : Test_InboundMemLeak::initTestCase()", run.StdOut);
            Assert.Contains("PASS   : Test_InboundMemLeak::loopInbound()", run.StdOut);
            Try("Known issue: QTBRIDGES-330", () =>
                Assert.Contains("PASS   : Test_InboundMemLeak::checkCorrelation()", run.StdOut));
            Assert.Contains("PASS   : Test_InboundMemLeak::cleanupTestCase()", run.StdOut);

            Console.WriteLine(run.StdOut);
        }
    }
}
