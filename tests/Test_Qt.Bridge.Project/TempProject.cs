// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using static System.IO.Directory;
using static System.IO.File;
using static System.IO.Path;

namespace Test_Qt.Bridge.Project
{
    public enum OutputType { Empty, Exe, WinExe }

    public class CreationOptions
    {
        public string Filename { get; set; }
        public string Extension { get; set; }
        public OutputType OutputType { get; set; } = OutputType.Exe;
        public string TargetFramework { get; set; }
        public bool ImplicitUsings { get; set; } = true;
        public bool Nullable { get; set; } = false;
        public int? QtBuildParallelism { get; set; } = 2;
        public List<(string Id, string Version)> PackageReferences { get; set; } = [];
        public string BeforeSdkProps { get; set; } = string.Empty;
        public string AfterSdkProps { get; set; } = string.Empty;
        public string BeforeSdkTargets { get; set; } = string.Empty;
        public string AfterSdkTargets { get; set; } = string.Empty;
        public bool LocalPackages { get; set; } = false;
        public IEnumerable<(string Old, string New)> ReplaceGeneratedFiles { get; set; } = [];
        public bool Reset { get; set; } = true;
    }

    public enum Config { Default, Debug, Release }

    public class BuildOptions
    {
        public Config Config { get; init; } = Config.Default;
        public bool? BinaryLog { get; init; }
        public bool? Restore { get; init; }
        public IEnumerable<string> Targets { get; init; } = [];
        public IEnumerable<(string Name, string Value)> Properties { get; init; } = [];
        public IEnumerable<string> OtherOptions { get; init; } = [];
        public int Timeout { get; init; } = -1;
        public string TargetPath { get; init; } = "TargetPath";
        public string TargetExePath { get; init; } = "RunCommand";
    }

    public enum Redirect { Nul, StdOut, StdErr }

    public class RunOptions
    {
        // Native integration tests launch a built executable and wait for it to exit; a hang in
        // the process under test must not hang the test run. Pass Timeout = -1 to opt out for a
        // run that is genuinely expected to take longer or run unbounded.
        public const int DefaultTimeout = 30000;

        public string ExePath { get; init; }
        public string WorkingDir { get; init; }
        public IEnumerable<string> Args { get; init; } = [];
        public IEnumerable<(string Name, string Value)> EnvVars { get; init; } = [];
        public Redirect StdOut { get; init; } = Redirect.StdOut;
        public Redirect StdErr { get; init; } = Redirect.StdErr;
        public int Timeout { get; init; } = DefaultTimeout;
    }

    public class TempProject : IDisposable
    {
        private const int MaxCapturedStreamChars = 1_000_000;
        private const int OutputDrainTimeoutMs = 2000;
        private const string CapturedStreamTruncationMessage =
            "... [output truncated by test harness] ...";

        private bool restoreCompleted;

        private const string TestRootEnvVar = "QTBRIDGE_TEST_ROOT";
        private const string TestRootDirName = "qtbridge-csharp-tests";
        private static readonly string TestSessionDirName = $"proc-{Environment.ProcessId}";

        // Resolve the temp project root from QTBRIDGE_TEST_ROOT or the system temp directory,
        // falling back to the project drive on Windows if needed, and fail fast on spaces.
        private static string ResolveSharedTestRootDir()
        {
            var rootDir = Environment.GetEnvironmentVariable(TestRootEnvVar);
            if (string.IsNullOrWhiteSpace(rootDir)) {
                rootDir = Path.GetTempPath();

                if (OperatingSystem.IsWindows() && rootDir.Contains(' ')) {
                    var projectDrive = Path.GetPathRoot(FindRepoRoot());
                    if (!string.IsNullOrWhiteSpace(projectDrive))
                        rootDir = projectDrive;
                }
            }

            if (rootDir.Contains(' ')) {
                throw new InvalidOperationException($"Temp test root contains spaces: '{rootDir}'. "
                    + $"Set {TestRootEnvVar} to a path without spaces.");
            }

            return Combine(rootDir, TestRootDirName);
        }

        private static string ResolveProjectRootDir() =>
            Combine(ResolveSharedTestRootDir(), TestSessionDirName);

        private static string ExecutingAssemblyDirectory =>
            GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

        public string ProjectRootDir { get; set; } = ResolveProjectRootDir();
        public string BinLogDir { get; set; } = Combine(ExecutingAssemblyDirectory, "logs");

        public string ProjectFilename { get; private set; } = GetRandomFileName();
        public string ProjectExtension { get; private set; } = ".csproj";

        public string ProjectDir => Combine(ProjectRootDir, ProjectFilename);
        public string ProjectPath => Combine(ProjectDir, ProjectFilename + ProjectExtension);
        private string BinLogPath => Combine(ProjectDir, "msbuild.binlog");
        public string NuGetPackagesDir => Combine(ProjectRootDir, "pkg");

        public Build Log => File.Exists(BinLogPath) ? BinaryLog.ReadBuild(BinLogPath) : new();

        public string ExePath { get; private set; }
        public string ExeDir => GetDirectoryName(ExePath);

        private static string NormalizeSeparators(string path) =>
            path.Replace('\\', DirectorySeparatorChar).Replace('/', DirectorySeparatorChar);

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(ExecutingAssemblyDirectory);
            while (dir != null) {
                if (File.Exists(Combine(dir.FullName, "qtbridge-csharp.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Could not locate repository root.");
        }

        private void WriteNuGetConfig()
        {
            var localFeed = NormalizeSeparators(Combine(FindRepoRoot(), "nuget", "local"));
            var globalPackages = NormalizeSeparators(NuGetPackagesDir);
            WriteAllText(Combine(ProjectDir, "nuget.config"),
                $"""
                 <?xml version="1.0" encoding="utf-8"?>
                 <configuration>
                   <config>
                     <add key="globalPackagesFolder" value="{globalPackages}" />
                   </config>
                   <packageSources>
                     <clear />
                     <add key="qtbridge-local" value="{localFeed}" />
                     <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                   </packageSources>
                   <packageSourceMapping>
                     <packageSource key="qtbridge-local">
                       <package pattern="QtGroup.Qt.Bridge.CSharp.*" />
                     </packageSource>
                     <packageSource key="nuget.org">
                       <package pattern="*" />
                     </packageSource>
                   </packageSourceMapping>
                 </configuration>
                 """);
        }

        public void Create(CreationOptions options = null)
        {
            options ??= new();
            Create(CreateProjectXml(options), options.Filename, options.Extension, options.Reset);
            WriteNuGetConfig();
        }

        /// <summary>
        /// Assembles the temporary SDK-style project file from XML fragments.
        /// </summary>
        private static string CreateProjectXml(CreationOptions options)
        {
            var sections = new[]
            {
                """<?xml version="1.0" encoding="utf-8"?>""",
                "<Project>",
                options.BeforeSdkProps,
                """  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />""",
                options.AfterSdkProps,
                PropertyGroupXml(options),
                PackageReferencesXml(options),
                options.BeforeSdkTargets,
                """  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />""",
                options.AfterSdkTargets,
                ReplaceGeneratedFilesTargetXml(options),
                "</Project>"
            };
            return string.Join(Environment.NewLine, sections
                .Where(section => !string.IsNullOrWhiteSpace(section)));
        }

        /// <summary>
        /// Creates the main property group, including local-package restore settings when the
        /// temp project should consume packages from the repo-local feed.
        /// </summary>
        private static string PropertyGroupXml(CreationOptions options)
        {
            var targetFramework = string.IsNullOrEmpty(options.TargetFramework)
                ? "net8.0"
                : options.TargetFramework;
            var lines = new List<string> { "  <PropertyGroup>" };
            if (OutputTypeXml(options.OutputType) is { Length: > 0 } outputType)
                lines.Add(outputType);
            lines.Add($"    <TargetFramework>{targetFramework}</TargetFramework>");
            lines.Add("    <RollForward>Major</RollForward>");
            lines.Add($"    <ImplicitUsings>{(options.ImplicitUsings ? "enable" : "disable")}"
                + "</ImplicitUsings>");
            lines.Add($"    <Nullable>{(options.Nullable ? "enable" : "disable")}</Nullable>");
            if (options.QtBuildParallelism > 0)
                lines.Add($"    <QtBuildParallelism>{options.QtBuildParallelism}"
                    + "</QtBuildParallelism>");
            lines.Add("  </PropertyGroup>");
            return string.Join(Environment.NewLine, lines);
        }

        private static string OutputTypeXml(OutputType outputType)
        {
            return outputType switch
            {
                OutputType.Exe => "    <OutputType>Exe</OutputType>",
                OutputType.WinExe => "    <OutputType>WinExe</OutputType>",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Creates the package reference item group for the temporary project.
        /// </summary>
        private static string PackageReferencesXml(CreationOptions options)
        {
            if (options.PackageReferences?.Any() != true)
                return string.Empty;

            var lines = new List<string> { "  <ItemGroup>" };
            lines.AddRange(options.PackageReferences.Select(x =>
                   $"""    <PackageReference Include="{x.Id}" Version="{x.Version}" />"""));
            lines.Add("  </ItemGroup>");
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Creates the post-codegen target that swaps selected generated native files with tests
        /// copied from the test assembly output.
        /// </summary>
        private static string ReplaceGeneratedFilesTargetXml(CreationOptions options)
        {
            if (options.ReplaceGeneratedFiles?.Any() != true)
                return string.Empty;

            var lines = new List<string>
            {
                """
                  <Target Name="QtPostCodeGen"
                """,
                """
                    AfterTargets="QtBridgeGenerate" BeforeTargets="QtBridgeBuild">
                """
            };

            var assemblyDir = ExecutingAssemblyDirectory;
            foreach (var file in options.ReplaceGeneratedFiles) {
                // Copy test assets into the generated qt/native tree before the native build runs.
                lines.Add("    <Copy");
                lines.Add(
                    $"""
                           SourceFiles="{NormalizeSeparators(Combine(assemblyDir, file.New))}"
                     """);
                lines.Add(
                    $"""
                            DestinationFiles="$(ProjectIntermediateDir){
                                NormalizeSeparators(Combine("qt", "native", file.Old))}" />
                     """);
            }
            lines.Add("  </Target>");
            return string.Join(Environment.NewLine, lines);
        }

        public void Create(
            string xml, string filename = null, string extension = null, bool reset = true)
        {
            if (reset)
                Reset();
            restoreCompleted = false;
            if (!string.IsNullOrEmpty(filename))
                ProjectFilename = filename;
            if (!string.IsNullOrEmpty(extension))
                ProjectExtension = extension;
            CreateDirectory(ProjectDir);
            WriteAllText(ProjectPath, xml);
        }

        public void Clone(string path)
        {
            if (path is not { Length: > 0 } || !File.Exists(path))
                throw new ArgumentException("Path cannot be null or empty and must exist.");
            var sourceDir = GetDirectoryName(path)
                ?? throw new ArgumentException("Path must include a parent directory.");
            Reset();
            ProjectFilename = GetFileNameWithoutExtension(path);
            ProjectExtension = GetExtension(path);
            CreateDirectory(ProjectDir);
            GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly)
                .ToList().ForEach(x => Copy(x, Combine(ProjectDir, GetFileName(x))));
        }

        public void CopyFile(string destinationPath, string sourcePath)
        {
            if (IsPathRooted(destinationPath) || IsPathRooted(sourcePath))
                throw new InvalidOperationException("Path must be relative.");
            destinationPath = NormalizeSeparators(destinationPath);
            sourcePath = NormalizeSeparators(sourcePath);
            Copy(
                Combine(ExecutingAssemblyDirectory, sourcePath),
                Combine(ProjectDir, destinationPath));
        }

        public void AddFile(string path, string contents)
        {
            if (IsPathRooted(path))
                throw new InvalidOperationException("Path must be relative.");
            path = NormalizeSeparators(path);
            var fullPath = Combine(ProjectDir, path);
            var directory = GetDirectoryName(fullPath)
                ?? throw new InvalidOperationException("Could not find target directory.");
            CreateDirectory(directory);
            WriteAllText(fullPath, contents);
        }

        private void Reset()
        {
            DeleteDirectory(ProjectDir);
            ProjectFilename = GetRandomFileName();
            ProjectExtension = ".csproj";
        }

        public void Dispose()
        {
#if !DEBUG
            Reset();
#endif
        }

        internal static void CleanupProjectRoot()
        {
            DeleteDirectory(ResolveProjectRootDir());
        }

        // Windows can hold files of a process for a while after it ended: an exiting app, error
        // reporting after a crash, a virus scanner. If they stay held, the directory is left
        // behind for the next cleanup. Cleanup does not decide whether a test passed, and an
        // exception from Dispose() would replace the test's own outcome.
        internal static bool DeleteDirectory(string dir, int timeoutMs = 10000)
        {
            var t = Stopwatch.StartNew();
            while (Directory.Exists(dir)) {
                try {
                    Delete(dir, true);
                } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
                    if (t.ElapsedMilliseconds > timeoutMs) {
                        Console.Error.WriteLine($"warning: left '{dir}' behind: {e.Message}");
                        return false;
                    }
                    Thread.Sleep(100);
                }
            }
            return true;
        }

        private (string Name, string Value)[] BuildEnvironment()
        {
            CreateDirectory(NuGetPackagesDir);
            return [("NUGET_PACKAGES", NuGetPackagesDir)];
        }

        private static List<string> PropertyArgs(BuildOptions options)
        {
            var args = new List<string>();
            switch (options.Config) {
                case Config.Debug:
                    args.Add("-p:Configuration=Debug");
                    break;
                case Config.Release:
                    args.Add("-p:Configuration=Release");
                    break;
            }

            if (options.Properties?.Any() != true)
                return args;

            foreach (var property in options.Properties)
                args.Add($"-p:{property.Name}={property.Value}");
            return args;
        }

        public async Task<(bool Ok, string Output)> BuildAsync(BuildOptions options = null)
        {
            options ??= new();
            ExePath = null;
            var args = new List<string>();
            var restore = options.Restore ?? !restoreCompleted;
            if (restore)
                args.Add("-restore");
            if (options.BinaryLog ?? AssemblyMetadata.Settings.BinaryLog)
                args.Add("-bl");
            var targets = options.Targets?.ToArray() ?? [];
            if (targets.Length > 0)
                args.Add($"-t:{string.Join(";", targets)}");
            args.AddRange(PropertyArgs(options));
            args.AddRange(options.OtherOptions);

            StringBuilder output = new();
            var msbuild = MsBuild.Start(
                stdOut => output.AppendLine(stdOut), stdErr => output.AppendLine(stdErr),
                ProjectDir, BuildEnvironment(), args.ToArray());
            CancellationTokenSource cancel = options.Timeout > 0 ? new(options.Timeout) : new();
            await msbuild.WaitForExitAsync(cancel.Token);
            if (msbuild.ExitCode != 0)
                return (false, output.ToString());
            if (restore || targets.Contains("Restore", StringComparer.OrdinalIgnoreCase))
                restoreCompleted = true;
            if (!string.IsNullOrEmpty(options.TargetPath)) {
                var targetPath = await GetPropertyAsync(options.TargetPath, options);
                if (!File.Exists(targetPath))
                    return (false, output.ToString());
            }

            if (string.IsNullOrEmpty(options.TargetExePath))
                return (true, output.ToString());

            var targetExePath = await GetPropertyAsync(options.TargetExePath, options);
            if (!File.Exists(targetExePath))
                return (false, output.ToString());
            ExePath = targetExePath;
            return (true, output.ToString());
        }

        public void SaveLog(string context = null, [CallerMemberName] string name = null)
        {
            context ??= "";
            name ??= ProjectFilename;
            if (!File.Exists(BinLogPath))
                return;
            CreateDirectory(BinLogDir);
            var logFileParts = new[] { name, context, ProjectFilename }
                .Where(part => !string.IsNullOrWhiteSpace(part));
            Copy(BinLogPath, Combine(BinLogDir, $"{string.Join("_", logFileParts)}.binlog"), true);
        }

        public async Task<string> GetPropertyAsync(string name, BuildOptions options = null)
        {
            options ??= new();
            var args = PropertyArgs(options);
            args.Add($"-getProperty:{name}");
            StringBuilder stdOut = new();
            var msbuild = MsBuild.Start(
                x => stdOut.AppendLine(x), null, ProjectDir, BuildEnvironment(), args.ToArray());
            CancellationTokenSource cancel = options.Timeout > 0 ? new(options.Timeout) : new();
            await msbuild.WaitForExitAsync(cancel.Token);

            return msbuild.ExitCode != 0 ? null : stdOut.ToString().Trim(' ', '\n', '\r', '\t');
        }

        private static void AppendCapturedText(StringBuilder output, char[] data, int count)
        {
            if (output == null || count <= 0)
                return;

            lock (output) {
                if (output.Length >= MaxCapturedStreamChars)
                    return;

                var remaining = MaxCapturedStreamChars - output.Length;
                if (remaining <= 0)
                    return;

                if (count <= remaining) {
                    output.Append(data, 0, count);
                    return;
                }

                var reservedForNotice = Environment.NewLine.Length
                    + CapturedStreamTruncationMessage.Length;
                var charsToCopy = Math.Max(0, remaining - reservedForNotice);
                if (charsToCopy > 0)
                    output.Append(data, 0, Math.Min(charsToCopy, count));
                if (output.Length < MaxCapturedStreamChars)
                    output.AppendLine();
                if (output.Length < MaxCapturedStreamChars)
                    output.Append(CapturedStreamTruncationMessage);
            }
        }

        private static async Task CaptureStreamAsync(StreamReader stream, StringBuilder output,
            CancellationToken cancellationToken)
        {
            var buffer = new char[4096];
            try {
                while (true) {
                    var count = await stream.ReadAsync(buffer, cancellationToken);
                    if (count == 0)
                        break;
                    AppendCapturedText(output, buffer, count);
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // A child process inherited the pipe and kept it open after the process under
                // test exited. Everything written before cancellation has already been captured.
            } catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) {
                // Closing the reader is the fallback for platforms where cancellation alone does
                // not interrupt an outstanding pipe read.
            }
        }

        private static string CapturedText(StringBuilder output)
        {
            lock (output)
                return output.ToString();
        }

        private static async Task WaitForProcessExitAsync(
            Process process, CancellationToken cancellationToken)
        {
            if (process.HasExited)
                return;

            var exited = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            void ProcessExited(object _, EventArgs __) => exited.TrySetResult();

            process.Exited += ProcessExited;
            try {
                process.EnableRaisingEvents = true;
                if (process.HasExited)
                    return;
                await exited.Task.WaitAsync(cancellationToken);
            } finally {
                process.Exited -= ProcessExited;
            }
        }

        private static async Task DrainProcessOutputAsync(Process process,
            CancellationTokenSource captureCancel, Task captureStdOut, Task captureStdErr)
        {
            using var drainTimeout = new CancellationTokenSource(OutputDrainTimeoutMs);
            var captureOutput = Task.WhenAll(captureStdOut, captureStdErr);
            try {
                await captureOutput.WaitAsync(drainTimeout.Token);
            } catch (OperationCanceledException) when (drainTimeout.IsCancellationRequested) {
                // The process itself has exited. A concurrently launched process may have
                // inherited one of its redirected pipe handles, preventing EOF indefinitely.
                captureCancel.Cancel();
                process.StandardOutput.Dispose();
                process.StandardError.Dispose();
                try {
                    await captureOutput;
                } catch (Exception exception) when (
                    exception is OperationCanceledException
                        or ObjectDisposedException
                        or IOException) {
                    // Expected when an inherited handle prevents normal EOF detection.
                }
            }
        }

        private static StringBuilder GetStreamBuffer(Redirect stream, StringBuilder stdOut,
            StringBuilder stdErr)
        {
            return stream switch
            {
                Redirect.StdOut => stdOut,
                Redirect.StdErr => stdErr,
                _ => null
            };
        }

        public async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
            RunOptions options = null)
        {
            options ??= new();
            var exePath = options.ExePath ?? ExePath;
            var workDir = options.WorkingDir ?? ProjectDir;
            var args = options.Args?.ToArray() ?? [];
            var envVars = options.EnvVars?.ToArray() ?? [];

            if (exePath == null)
                throw new InvalidOperationException("Missing executable. Did you forget to build?");

            StringBuilder stdOut = new(), stdErr = new();
            using var run = CmdProc.StartRaw(exePath, workDir, args, envVars);
            using var captureCancel = new CancellationTokenSource();
            var captureStdOut = CaptureStreamAsync(run.StandardOutput,
                GetStreamBuffer(options.StdOut, stdOut, stdErr), captureCancel.Token);
            var captureStdErr = CaptureStreamAsync(run.StandardError,
                GetStreamBuffer(options.StdErr, stdOut, stdErr), captureCancel.Token);
            using CancellationTokenSource cancel = options.Timeout > 0
                ? new(options.Timeout)
                : new();
            try {
                await WaitForProcessExitAsync(run, cancel.Token);
            } catch (OperationCanceledException exception) when (cancel.IsCancellationRequested) {
                // The timeout and process-exit signals can race. If the process won, only its
                // redirected streams still need attention; it is not a process timeout.
                if (run.HasExited) {
                    await DrainProcessOutputAsync(
                        run, captureCancel, captureStdOut, captureStdErr);
                    return (run.ExitCode, CapturedText(stdOut), CapturedText(stdErr));
                }

                try {
                    run.Kill(true);
                } catch {
                    // Ignore; try making the original timeout details below visible.
                }

                await DrainProcessOutputAsync(run, captureCancel, captureStdOut, captureStdErr);

                throw new TimeoutException(
                    $"Timed out waiting for process to exit after {options.Timeout} ms."
                    + $"{Environment.NewLine}ExePath: {exePath}"
                    + $"{Environment.NewLine}WorkingDir: {workDir}"
                    + $"{Environment.NewLine}Args: {string.Join(" ", args)}"
                    + $"{Environment.NewLine}EnvVars: {
                        string.Join("; ", envVars.Select(x => $"{x.Name}={x.Value}"))
                    }"
                    + $"{Environment.NewLine}StdOut:{Environment.NewLine}{CapturedText(stdOut)}"
                    + $"{Environment.NewLine}StdErr:{Environment.NewLine}{CapturedText(stdErr)}",
                    exception);
            }
            await DrainProcessOutputAsync(run, captureCancel, captureStdOut, captureStdErr);
            return (run.ExitCode, CapturedText(stdOut), CapturedText(stdErr));
        }
    }
}
