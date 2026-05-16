// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;

namespace Test_Qt.Bridge.Project
{
    public static class MsBuild
    {
        public static string MsBuildPath { get; private set; }

        private static readonly object criticalSection = new();
        private static Exception initError = null;

        private static void CheckInstallPath(string path)
        {
            if (File.Exists(MsBuildPath))
                return;
            if (!Directory.Exists(path))
                return;
            var test = Path.Combine(path, "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe");
            if (!File.Exists(test))
                return;
            MsBuildPath = test;
        }

        private static void Init()
        {
            lock (criticalSection) {
                if (File.Exists(MsBuildPath))
                    return;
                if (!OperatingSystem.IsWindows()) {
                    MsBuildPath = "dotnet";
                    return;
                }
                if (initError != null)
                    throw initError;
                var vswherePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft Visual Studio", "Installer", "vswhere.exe");
                if (!File.Exists(vswherePath))
                    throw initError = new InvalidOperationException("VS Locator tool not found.");

                CmdProc.Start(vswherePath, Environment.CurrentDirectory, stdOut: CheckInstallPath,
                    args: ["-version", "[17.0,18.0)", "-latest", "-property", "installationPath"])
                    .WaitForExit();

                if (!File.Exists(MsBuildPath))
                    throw initError = new InvalidOperationException("MSBuild tool not found.");
            }
        }

        public static Process Start(
            string workDir, (string Name, string Value)[] envVars = null, params string[] args)
        {
            Init();
            return CmdProc.Start(MsBuildPath, workDir,
                MsBuildPath == "dotnet" ? ["msbuild", .. args] : args,
                envVars);
        }

        public static Process Start(
            Action<string> stdOut, Action<string> stdErr, string workDir,
            (string Name, string Value)[] envVars = null, params string[] args)
        {
            Init();
            return CmdProc.Start(MsBuildPath, workDir,
                MsBuildPath == "dotnet" ? ["msbuild", .. args] : args,
                envVars, stdOut, stdErr);
        }

        public static string Evaluate(string msbuildExpr,
            string workDir, (string Name, string Value)[] envVars = null, params string[] args)
        {
            return Evaluate(Timeout.Infinite, msbuildExpr, workDir, envVars, args);
        }

        public static string Evaluate(int timeout, string msbuildExpr,
            string workDir, (string Name, string Value)[] envVars = null, params string[] args)
        {
            if (!Monitor.TryEnter(criticalSection_Evaluate, timeout))
                throw new TimeoutException("Timed out waiting for MSBuild expression eval.");

            bool createdTargetFile = false;
            var targetFile = Path.GetFullPath(Path.Combine(workDir, "Directory.Build.targets"));
            try {
                if (File.Exists(targetFile))
                    throw new InvalidOperationException($"Directory.Build.targets in {workDir}");

                File.WriteAllText(targetFile,
                    $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Project>
                  <Target Name="EvaluateExpression">
                    <PropertyGroup>
                      <ExpressionValue>{SecurityElement.Escape(msbuildExpr)}</ExpressionValue>
                    </PropertyGroup>
                  </Target>
                </Project>
                """);
                createdTargetFile = true;

                var stdOut = new StringBuilder();
                var stdErr = new StringBuilder();
                args = args
                    .Append("-t:EvaluateExpression")
                    .Append("-getProperty:ExpressionValue")
                    .ToArray();
                var msbuildProc = Start(
                    x => stdOut.AppendLine(x), x => stdErr.AppendLine(x), workDir, envVars, args);
                msbuildProc.WaitForExit();

                File.Delete(targetFile);

                if (msbuildProc.ExitCode != 0) {
                    throw new Exception($"""
                        Error evaluating MSBuild expression
                        ----
                        {stdOut.ToString()}
                        ----
                        {stdErr.ToString()}
                        """);
                }
                return stdOut.ToString().Trim(' ', '\r', '\n');

            } finally {
                if (createdTargetFile)
                    File.Delete(targetFile);
                Monitor.Exit(criticalSection_Evaluate);
            }
        }

        private static readonly object criticalSection_Evaluate = new();
    }
}
