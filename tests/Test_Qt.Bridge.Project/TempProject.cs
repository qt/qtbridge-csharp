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
        public string Filename { get; init; }
        public string Extension { get; init; }
        public OutputType OutputType { get; init; } = OutputType.Exe;
        public string TargetFramework { get; init; }
        public bool ImplicitUsings { get; init; } = true;
        public bool Nullable { get; init; } = false;
        public IEnumerable<(string Id, string Version)> PackageReferences { get; init; } = [];
        public string BeforeSdkProps { get; init; } = string.Empty;
        public string AfterSdkProps { get; init; } = string.Empty;
        public string BeforeSdkTargets { get; init; } = string.Empty;
        public string AfterSdkTargets { get; init; } = string.Empty;
        public bool LocalPackages { get; init; } = false;
        public IEnumerable<(string Old, string New)> ReplaceGeneratedFiles { get; init; } = [];
    }

    public enum Config { Default, Debug, Release }

    public class BuildOptions
    {
        public Config Config { get; init; } = Config.Default;
        public bool BinaryLog { get; init; } = true;
        public bool Restore { get; init; } = true;
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
        public string ExePath { get; init; }
        public string WorkingDir { get; init; }
        public IEnumerable<string> Args { get; init; } = [];
        public IEnumerable<(string Name, string Value)> EnvVars { get; init; } = [];
        public Redirect StdOut { get; init; } = Redirect.StdOut;
        public Redirect StdErr { get; init; } = Redirect.StdErr;
        public int Timeout { get; init; } = -1;
    }

    public class TempProject : IDisposable
    {
        private const string TestRootEnvVar = "QTBRIDGE_TEST_ROOT";
        private const string TestRootDirName = "qtbridge-csharp-tests";

        // Resolve the temp project root from QTBRIDGE_TEST_ROOT or the system temp directory,
        // falling back to the project drive on Windows if needed, and fail fast on spaces.
        private static string ResolveProjectRootDir()
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
                   </packageSources>
                 </configuration>
                 """);
        }

        public void Create(CreationOptions options = null)
        {
            options ??= new();
            Create(CreateProjectXml(options), options.Filename, options.Extension);
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
            lines.Add($"    <ImplicitUsings>{(options.ImplicitUsings ? "enable" : "disable")}"
                + "</ImplicitUsings>");
            lines.Add($"    <Nullable>{(options.Nullable ? "enable" : "disable")}</Nullable>");
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

        public void Create(string xml, string filename = null, string extension = null)
        {
            Reset();
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
            var projectDir = ProjectDir;
            var t = Stopwatch.StartNew();
            while (Directory.Exists(projectDir)) {
                try {
                    Delete(projectDir, true);
                } catch (IOException e) {
                    if (!e.Message.Contains("being used by another process"))
                        throw;
                    if (t.ElapsedMilliseconds > 10000)
                        Assert.Inconclusive(e.Message);
                    Thread.Sleep(100);
                }
            }
            ProjectFilename = GetRandomFileName();
            ProjectExtension = ".csproj";
        }

        public void Dispose()
        {
            Reset();
        }

        internal static void CleanupProjectRoot()
        {
            var projectRootDir = ResolveProjectRootDir();
            var t = Stopwatch.StartNew();
            while (Directory.Exists(projectRootDir)) {
                try {
                    Delete(projectRootDir, true);
                } catch (IOException e) {
                    if (!e.Message.Contains("being used by another process"))
                        throw;
                    if (t.ElapsedMilliseconds > 10000)
                        Assert.Inconclusive(e.Message);
                    Thread.Sleep(100);
                }
            }
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
            if (options.Restore)
                args.Add("-restore");
            if (options.BinaryLog)
                args.Add("-bl");
            if (options.Targets?.Any() == true)
                args.Add($"-t:{string.Join(";", options.Targets)}");
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
            var sep = string.IsNullOrEmpty(context) ? "" : "_";
            Copy(BinLogPath, Combine(BinLogDir, $"{name}{sep}{context}.binlog"), true);
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

        private static Action<string> GetStreamHandler(Redirect stream, StringBuilder stdOut,
            StringBuilder stdErr)
        {
            return stream switch
            {
                Redirect.StdOut => data => stdOut.AppendLine(data),
                Redirect.StdErr => data => stdErr.AppendLine(data),
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
            var run = CmdProc.Start(exePath, workDir, args, envVars,
                GetStreamHandler(options.StdOut, stdOut, stdErr),
                GetStreamHandler(options.StdErr, stdOut, stdErr));
            CancellationTokenSource cancel = options.Timeout > 0 ? new(options.Timeout) : new();
            await run.WaitForExitAsync(cancel.Token);
            return (run.ExitCode, stdOut.ToString(), stdErr.ToString());
        }
    }
}
