/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Test_Qt.DotNet.Project
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

            var build = await temp.BuildAsync(new() { BinaryLog = false });
            temp.SaveLog();
            Assert.IsTrue(build.Ok);

            var run = await temp.RunAsync();
            Assert.IsTrue(run.ExitCode == 0);
            Assert.Contains("Hello World!", run.StdOut);
        }

        [TestMethod
            , DataRow("msbuild")
            , DataRow("ninja")
        ]
        public async Task HelloWorld_NullCodeGen(string buildSystem)
        {
            using var temp = new TempProject();
            temp.Create(new()
            {
                PackageReferences = [Packages.QtBridge]
            });
            temp.AddFile("Program.cs", Source);

            var build = await temp.BuildAsync(new()
            {
                Properties = [("QtBuildSystem", buildSystem)]
            });
            temp.SaveLog(buildSystem);
            Assert.IsTrue(build.Ok);

            Assert.IsTrue(temp.Log.TryFindTarget("QtDotNetBuild", out var qtDotNetBuild));
            string[] cppFiles = [
                "convert.cpp",
                "main.cpp",
                "object_dispatch.cpp",
                "typecast.cpp"
            ];
            if (buildSystem == "ninja") {
                var msgs = qtDotNetBuild.GetMessages();
                foreach (var cppFile in cppFiles)
                    Assert.Contains(m => Regex.IsMatch(m, @"\bBuilding CXX .*\b" + cppFile), msgs);
            } else {
                var clSourceFilenames = qtDotNetBuild.GetTasks("CL")
                    .SelectMany(cl => cl.GetParamItems("Sources"))
                    .Select(src => Path.GetFileName(src.Text));
                foreach (var cppFile in cppFiles)
                    Assert.Contains(cppFile, clSourceFilenames);
            }

            var run = await temp.RunAsync();
            Assert.IsTrue(run.ExitCode == 0);
            Assert.Contains("Hello World!", run.StdOut);
        }
    }
}
