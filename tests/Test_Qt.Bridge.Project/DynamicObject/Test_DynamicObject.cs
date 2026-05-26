// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project.DynamicObject
{
    using static AssemblyMetadata;

    [TestClass]
    public class Test_DynamicObject : ManagedTestBase
    {
        private static MetadataLoadContext Loader {  get; set; }
        private static string PrimesDll { get; set; }
        private static Assembly Primes { get; set; }
        private static TempProject Temp { get; set; }
        private static (string, string)[] DefaultEnvVars =
        [
            ("QT_FORCE_STDERR_LOGGING", "1"),
            ("QML_DISABLE_DISK_CACHE", "1")
        ];

        [ClassInitialize]
        public static async Task InitAsync(TestContext context)
        {
            var primesDir = Path.Combine(Build.ProjectDir, "..", "..", "examples", "Primes");
            var primesBuild = MsBuild.Start(primesDir,
                [ ("Configuration", Build.Configuration), ("Platform", Build.Platform),
                ("DesignTimeBuild", "true") ], "-restore", "-t:Compile");

            await primesBuild.WaitForExitAsync(context.CancellationTokenSource.Token);
            Assert.AreEqual(0, primesBuild.ExitCode);

            PrimesDll = MsBuild.Evaluate("$(ProjectDir)@(IntermediateAssembly)", primesDir,
                [("Configuration", Build.Configuration), ("Platform", Build.Platform)]);
            Assert.IsNotEmpty(PrimesDll);
            Assert.IsTrue(File.Exists(PrimesDll));

            Temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine("DynamicObject", "main.cpp"));
            await InitializeAndBuildAsync(Temp, options,
                project =>
                {
                    project.CopyFile("Program.cs", Path.Combine(
                        "DynamicObject", "Program.cs"));
                    project.CopyFile("tst_callmethod.qml", Path.Combine(
                        "DynamicObject", "tst_callmethod.qml"));
                    project.CopyFile("tst_property.qml", Path.Combine(
                        "DynamicObject", "tst_property.qml"));
                });

            File.Copy(PrimesDll, Path.Combine(Temp.ExeDir, "Primes.dll"));

            var assemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
                .Union(Directory.GetFiles(Environment.CurrentDirectory, "*.dll"))
                .Union(Directory.GetFiles(Temp.ExeDir, "*.dll"))
                .ToArray();
            Loader = new MetadataLoadContext(new PathAssemblyResolver(assemblies));
            Primes = Loader.LoadFromAssemblyName("Primes");
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            Loader?.Dispose();
            Temp?.Dispose();
        }

        public TestContext TestContext { get; set; }

        private void CheckTestRun((int ExitCode, string StdOut, string StdErr) testRun)
        {
            var msgs = ParseQtTestMessages(testRun.StdOut);
            msgs.Fail.ForEach(msg => TestContext.WriteLine(msg));
            msgs.Warning.ForEach(msg => TestContext.WriteLine(msg));

            Assert.AreEqual((int)ExitCode.Ok, testRun.ExitCode,
                $"""
                {ExitCodeHelper.ToString(testRun.ExitCode)}
                {(msgs.Fail.Any() ? string.Join(Environment.NewLine, msgs.Fail) : testRun.StdOut)}
                """);
            Assert.IsEmpty(msgs.Fail, string.Join(Environment.NewLine, msgs.Fail));
            Assert.IsNotEmpty(msgs.Pass);
        }

        [TestMethod]
        public async Task Create_Object_And_Call_Instance_Method()
        {
            var primeFactory = Primes.GetType("PrimesApp.PrimeFactory");
            Assert.IsNotNull(primeFactory);

            var getNthPrime = primeFactory.GetMethod("GetNthPrime");
            Assert.IsNotNull(getNthPrime);

            CheckTestRun(await Temp.RunAsync(new()
            {
                WorkingDir = Temp.ExeDir,
                Args = ["-input", Path.Combine(Temp.ExeDir, "Application", "tst_callmethod.qml")],
                EnvVars = DefaultEnvVars.Union([
                    ("PrimesApp_PrimeFactory_GetNthPrime", getNthPrime.MetadataToken.ToString())
                ]),
                StdErr = Redirect.StdOut
            }));
        }

        [TestMethod]
        public async Task Read_Write_Property()
        {
            CheckTestRun(await Temp.RunAsync(new()
            {
                WorkingDir = Temp.ExeDir,
                Args = ["-input", Path.Combine(Temp.ExeDir, "Application", "tst_property.qml")],
                EnvVars = DefaultEnvVars,
                StdErr = Redirect.StdOut
            }));
        }
    }
}
