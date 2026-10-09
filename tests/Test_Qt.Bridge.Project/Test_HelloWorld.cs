// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public class Test_HelloWorld
    {
        private string Source = $@"
namespace HelloWorld
{{
    internal class Program
    {{
        static int Main(string[] args)
        {{
            Console.WriteLine(""Hello World!"");
            return 0;
        }}
    }}
}}
";
        [TestMethod]
        public async Task HelloWorld_NoCodeGen()
        {
            using var temp = new TempProject();
            temp.Create();
            temp.AddFile("Program.cs", Source);

            var build = await temp.BuildAsync();
            temp.SaveLog();
            Assert.IsTrue(build.Ok);

            var run = await temp.RunAsync();
            Assert.IsTrue(run.ExitCode == 0);
            Assert.Contains("Hello World!", run.StdOut);
        }

        [TestMethod]
        public async Task HelloWorld_NullCodeGen()
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge]
            });
            temp.AddFile("Program.cs", Source);

            var build = await temp.BuildAsync(new() { BinaryLog = true });
            temp.SaveLog();
            Assert.IsTrue(build.Ok, build.Output);

            Assert.IsTrue(temp.Log.TryFindTarget("QtBridgeBuild", out var qtDotNetBuild));
            string[] cppFiles = [
                "main.cpp",
                "object_dispatch.cpp",
                "typecast.cpp"
            ];
            var msgs = qtDotNetBuild.GetMessages();
            foreach (var cppFile in cppFiles)
                Assert.Contains(m => Regex.IsMatch(m, @"\bBuilding CXX .*\b" + cppFile), msgs);

            var run = await temp.RunAsync();
            Assert.IsTrue(run.ExitCode == 0,
                $"stdout:{Environment.NewLine}{run.StdOut}{Environment.NewLine}"
                + $"stderr:{Environment.NewLine}{run.StdErr}");
            Assert.Contains("Hello World!", run.StdOut);
        }
    }
}
