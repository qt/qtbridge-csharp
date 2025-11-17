/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;

using static System.IO.Path;
using static System.IO.File;
using static System.IO.Directory;

namespace Test_Qt.DotNet.Project
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

        public string ProjectRootDir { get; set; }
            = Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location), "temp");
        public string BinLogDir { get; set; }
            = Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");

        public string ProjectFilename { get; private set; } = GetRandomFileName();
        public string ProjectExtension { get; private set; } = ".csproj";

        public string ProjectDir => Combine(ProjectRootDir, ProjectFilename);
        public string ProjectPath => Combine(ProjectDir, ProjectFilename + ProjectExtension);
        private string BinLogPath => Combine(ProjectDir, "msbuild.binlog");

        public Build Log => File.Exists(BinLogPath) ? BinaryLog.ReadBuild(BinLogPath) : new();

        public string ExePath { get; private set; }
        public string ExeDir => GetDirectoryName(ExePath);

        public void Create(CreationOptions options = null)
        {
            options ??= new();
            Create($@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project>
{options.BeforeSdkProps}
  <Import Project=""Sdk.props"" Sdk=""Microsoft.NET.Sdk"" />
{options.AfterSdkProps}
  <PropertyGroup>
    {options.OutputType switch
            {
                OutputType.Exe => "<OutputType>Exe</OutputType>",
                OutputType.WinExe => "<OutputType>WinExe</OutputType>",
                _ => ""
            }}
    <TargetFramework>{options.TargetFramework switch
            {
                { Length: > 0 } => options.TargetFramework,
                _ => "net8.0"
            }}</TargetFramework>
    <ImplicitUsings>{(options.ImplicitUsings ? "enable" : "disable")}</ImplicitUsings>
    <Nullable>{(options.Nullable ? "enable" : "disable")}</Nullable>
  </PropertyGroup>
  {(options.PackageReferences?.Any() == true
    ? $@"<ItemGroup>
        {string.Join(@"
        ", options.PackageReferences
            .Select(x => $@"<PackageReference Include=""{x.Id}"" Version=""{x.Version}"" />"))}
  </ItemGroup>" : "")}
{options.BeforeSdkTargets}
  <Import Project=""Sdk.targets"" Sdk=""Microsoft.NET.Sdk"" />
{options.AfterSdkTargets}
{(options.ReplaceGeneratedFiles?.Any() == true ? $@"
  <Target Name=""QtPostCodeGen""
    AfterTargets=""QtDotNetGenerate"" BeforeTargets=""QtDotNetBuild"">
    {string.Join(@"
    ", options.ReplaceGeneratedFiles
        .Select(x => $@"<Copy
      SourceFiles=""{GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\{x.New}""
      DestinationFiles=""$(ProjectIntermediateDir)qtdotnet\native\{x.Old}"" />"))}
  </Target>" : "")}
</Project>
".Trim(' ', '\r', '\n'), options.Filename, options.Extension);
            if (options.LocalPackages) {
                WriteAllText($@"{ProjectDir}\nuget.config",
                    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <config>
    <add key=""globalPackagesFolder"" value=""packages"" />
  </config>
</configuration>
");
            }
        }

        public void Create(string xml, string filename = null, string extension = null)
        {
            Reset();
            if (!string.IsNullOrEmpty(filename))
                ProjectFilename = filename;
            if (!string.IsNullOrEmpty(extension))
                ProjectFilename = extension;
            CreateDirectory(ProjectDir);
            WriteAllText(ProjectPath, xml);
        }

        public void Clone(string path)
        {
            if (path is not { Length: > 0 } || !File.Exists(path))
                throw new ArgumentException();
            Reset();
            ProjectFilename = GetFileNameWithoutExtension(path);
            ProjectExtension = GetExtension(path);
            CreateDirectory(ProjectDir);
            GetFiles(GetDirectoryName(path), "*", SearchOption.TopDirectoryOnly)
                .ToList().ForEach(x => Copy(x, Combine(ProjectDir, GetFileName(x))));
        }

        public void CopyFile(string destinationPath, string sourcePath)
        {
            if (IsPathRooted(destinationPath) || IsPathRooted(sourcePath))
                throw new InvalidOperationException("Path must be relative.");
            Copy(
                Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location), sourcePath),
                Combine(ProjectDir, destinationPath));
        }

        public void AddFile(string path, string contents)
        {
            if (IsPathRooted(path))
                throw new InvalidOperationException("Path must be relative.");
            string fullPath = Combine(ProjectDir, path);
            CreateDirectory(GetDirectoryName(fullPath));
            WriteAllText(fullPath, contents);
        }

        private void Reset()
        {
            ProjectFilename = GetRandomFileName();
            ProjectExtension = ".csproj";
            var t = Stopwatch.StartNew();
            while (Directory.Exists(ProjectDir)) {
                try {
                    Delete(ProjectDir, true);
                } catch (IOException e) {
                    if (!e.Message.Contains("being used by another process"))
                        throw;
                    if (t.ElapsedMilliseconds > 10000)
                        Assert.Inconclusive(e.Message);
                    Thread.Sleep(100);
                }
            }
        }

        public void Dispose()
        {
            Reset();
        }

        private List<string> PropertyArgs(BuildOptions options)
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
            if (options.Properties?.Any() == true) {
                foreach (var property in options.Properties)
                    args.Add($"-p:{property.Name}={property.Value}");
            }
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
                ProjectDir, args.ToArray());
            CancellationTokenSource cancel = options.Timeout > 0 ? new(options.Timeout) : new();
            await msbuild.WaitForExitAsync(cancel.Token);
            if (msbuild.ExitCode != 0)
                return (false, output.ToString());
            if (!string.IsNullOrEmpty(options.TargetPath)) {
                var targetPath = await GetPropertyAsync(options.TargetPath, options);
                if (!File.Exists(targetPath))
                    return (false, output.ToString());
            }
            if (!string.IsNullOrEmpty(options.TargetExePath)) {
                var targetExePath = await GetPropertyAsync(options.TargetExePath, options);
                if (!File.Exists(targetExePath))
                    return (false, output.ToString());
                ExePath = targetExePath;
            }
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
                x => stdOut.AppendLine(x), null, ProjectDir, args.ToArray());
            CancellationTokenSource cancel = options.Timeout > 0 ? new(options.Timeout) : new();
            await msbuild.WaitForExitAsync(cancel.Token);
            if (msbuild.ExitCode != 0)
                return null;
            return stdOut.ToString().Trim(' ', '\n', '\r', '\t');
        }

        private Action<string> FnStream(Redirect stream, StringBuilder stdOut, StringBuilder stdErr)
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
            var args = options.Args?.ToArray() ?? new string[0];
            var envVars = options.EnvVars?.ToArray() ?? new (string, string)[0];

            if (exePath == null)
                throw new InvalidOperationException("Missing executable. Did you forget to build?");

            StringBuilder stdOut = new(), stdErr = new();
            var run = CmdProc.Start(exePath, workDir, args, envVars,
                FnStream(options.StdOut, stdOut, stdErr), FnStream(options.StdErr, stdOut, stdErr));
            CancellationTokenSource cancel = options.Timeout > 0 ? new(options.Timeout) : new();
            await run.WaitForExitAsync(cancel.Token);
            return (run.ExitCode, stdOut.ToString(), stdErr.ToString());
        }
    }
}
