// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;

namespace Qt.Bridge.Utils.Concurrent
{
    public static class CmdProc
    {
        public static Process Start(
            string exePath, string workDir, string[] args,
            Action<string> stdOut = null, Action<string> stdErr = null)
        {
            return Start(exePath, workDir, args, null, stdOut, stdErr);
        }

        public static Process Start(
            string exePath, string workDir, string[] args, (string Name, string Value)[] envVars,
            Action<string> stdOut = null, Action<string> stdErr = null)
        {
            var proc = StartRaw(exePath, workDir, args, envVars);
            stdOut ??= data => Debug.WriteLine(data);
            stdErr ??= data => Debug.WriteLine(data);
            proc.OutputDataReceived += (_, ev) => stdOut(ev.Data);
            proc.ErrorDataReceived += (_, ev) => stdErr(ev.Data);
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            return proc;
        }

        public static Process StartRaw(
            string exePath, string workDir, string[] args, (string Name, string Value)[] envVars)
        {
            if (exePath == null)
                throw new ArgumentNullException(nameof(exePath));
            var isPathLike = Path.IsPathRooted(exePath)
                || exePath.Contains(Path.DirectorySeparatorChar)
                || exePath.Contains(Path.AltDirectorySeparatorChar);
            if (isPathLike && !File.Exists(exePath))
                throw new InvalidOperationException($"File not found: '{exePath}'");

            if (workDir == null)
                throw new ArgumentNullException(nameof(workDir));
            if (!Directory.Exists(workDir))
                throw new InvalidOperationException($"Directory not found: '{workDir}'");

            args ??= [];
            envVars ??= [];
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = workDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            foreach (var arg in args)
                proc.StartInfo.ArgumentList.Add(arg);
            foreach (var envVar in envVars)
                proc.StartInfo.Environment[envVar.Name] = envVar.Value;
            if (!proc.Start())
                throw new InvalidOperationException($"Could not start process: '{exePath}'.");
            return proc;
        }
    }
}
