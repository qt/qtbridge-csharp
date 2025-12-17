/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace Test_Qt.Bridge.Project
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
            if (exePath == null)
                throw new ArgumentNullException(nameof(exePath));
            if (!File.Exists(exePath))
                throw new InvalidOperationException($"File not found: '{exePath}'");

            if (workDir == null)
                throw new ArgumentNullException(nameof(workDir));
            if (!Directory.Exists(workDir))
                throw new InvalidOperationException($"Directory not found: '{workDir}'");

            args ??= [];
            envVars ??= [];
            stdOut ??= data => Debug.WriteLine(data);
            stdErr ??= data => Debug.WriteLine(data);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = string.Join(" ", args
                        .Where(arg => arg is { Length: > 0 })
                        .Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                    WorkingDirectory = workDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };
            foreach (var envVar in envVars)
                proc.StartInfo.Environment[envVar.Name] = envVar.Value;
            proc.OutputDataReceived += (_, ev) => stdOut(ev.Data);
            proc.ErrorDataReceived += (_, ev) => stdErr(ev.Data);
            if (!proc.Start())
                throw new InvalidOperationException($"Could not start process: '{exePath}'.");
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            return proc;
        }
    }
}
