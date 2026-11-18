// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Qt.Bridge.CodeGeneration
{
    using Utils.Concurrent;

    public abstract partial class Rule
    {
        public static class QtCmd
        {
            public static Process Start(
                string tool, string workDir, string[] args,
                Action<string> stdOut = null, Action<string> stdErr = null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(workDir);
                return CmdProc.Start(FindTool(tool), workDir, args, stdOut, stdErr);
            }

            internal static string FindTool(string tool)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(tool);

                if (Rules.QtDir is not { Exists: true } && Rules.QtLibDir is not { Exists: true })
                    throw new InvalidOperationException("QtDir/QtLibDir not set.");

                if (!tool.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    tool = $"{tool}{(OperatingSystem.IsWindows() ? ".exe" : "")}";

                var paths = new List<string>();
                if (Rules.QtDir is { Exists: true }) {
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "bin", tool));
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "libexec", tool));
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "lib", "qt6", "bin", tool));
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "lib", "qt6", "libexec", tool));
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "lib", "qt6", tool));
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "lib64", "qt6", "bin", tool));
                    paths.Add(Path.Combine(Rules.QtDir.FullName, "lib64", "qt6", "libexec", tool));
                }
                if (Rules.QtLibDir is { Exists: true }) {
                    paths.Add(Path.Combine(Rules.QtLibDir.FullName, "qt6", "bin", tool));
                    paths.Add(Path.Combine(Rules.QtLibDir.FullName, "qt6", "libexec", tool));
                    paths.Add(Path.Combine(Rules.QtLibDir.FullName, "qt6", tool));
                }

                if (paths.FirstOrDefault(File.Exists) is { Length: > 0 } toolPath)
                    return toolPath;
                throw new ArgumentException($"Qt tool '{tool}' not found.", nameof(tool));
            }

            internal static async Task WaitForExitAsync(Process proc, TimeSpan timeout)
            {
                using var cancellation = new CancellationTokenSource(timeout);
                try {
                    await proc.WaitForExitAsync(cancellation.Token);
                } catch (OperationCanceledException e) when (cancellation.IsCancellationRequested) {
                    if (!proc.HasExited) {
                        try {
                            proc.Kill(entireProcessTree: true);
                        } catch (InvalidOperationException) when (proc.HasExited) {
                        }
                        await proc.WaitForExitAsync(CancellationToken.None);
                    }
                    throw new TimeoutException($"Qt tool '{proc.StartInfo.FileName}' timed out", e);
                }
            }
        }

        public class QmlDom
        {
            public bool IsSingleton { get; private set; }

            private QmlDom() { }

            public static async Task<QmlDom> ExecAsync(
                string qmlFilePath, params string[] disableFields)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(qmlFilePath);

                if (!File.Exists(qmlFilePath))
                    throw new FileNotFoundException("QML file not found", qmlFilePath);

                var args = new List<string>();
                if (disableFields is { Length: > 0 }) {
                    args.Add("-f");
                    args.Add($"{string.Join(",", disableFields.Select(field => $"-:{field}"))}");
                }
                args.Add($"{qmlFilePath}");

                var stdOut = new StringBuilder();
                var stdErr = new StringBuilder();
                using var qmlDomProc = QtCmd.Start("qmldom", Environment.CurrentDirectory,
                    [.. args], str => stdOut.AppendLine(str), str => stdErr.AppendLine(str));
                await QtCmd.WaitForExitAsync(qmlDomProc, TimeSpan.FromSeconds(5));
                if (qmlDomProc.ExitCode != 0) {
                    throw new InvalidOperationException($"qmldom failed for '{qmlFilePath}' "
                        + $"({qmlDomProc.ExitCode}){Environment.NewLine}{stdErr}");
                }
                var qmlDom = new QmlDom();
                try {
                    using var jsonText = new MemoryStream(Encoding.UTF8.GetBytes(stdOut.ToString()));
                    using var qmlDomJson = await JsonDocument.ParseAsync(jsonText);
                    qmlDom.IsSingleton = qmlDomJson.RootElement
                        .GetProperty("currentItem")
                        .GetProperty("components")
                        .GetProperty("")[0]
                        .GetProperty("isSingleton")
                        .GetBoolean();
                } catch (Exception e) when (e is JsonException or KeyNotFoundException
                    or InvalidOperationException or IndexOutOfRangeException) {
                    throw new InvalidDataException($"Failed to parse qmldom output: {qmlFilePath}", e);
                }
                return qmlDom;
            }

            public static QmlDom Exec(string qmlFilePath, params string[] disableFields)
            {
                return ExecAsync(qmlFilePath, disableFields).GetAwaiter().GetResult();
            }
        }
    }
}
