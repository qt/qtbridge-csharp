// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Threading;

namespace Test_Qt.Bridge.Project
{
    using static Qt.Bridge.CSharp.Build.Tasks.PatchNativeHostManifest;

    [TestClass]
    public class Test_NativeHostManifest
    {
        public TestContext TestContext { get; set; }
        private CancellationToken Token => TestContext.CancellationTokenSource.Token;

        private static readonly byte[] ManifestHeader =
            [(byte)'Q', (byte)'T', (byte)'B', (byte)'M', 1, 0,
                ManifestPayloadSize & 0xff,
                ManifestPayloadSize >> 8];

        private const string MetadataFileName = "qt_bridge_metadata_dummy_name.json";

        private const string Source = """
             using Qt.Bridge.Models;
             using Qt.Quick;

             namespace ManifestManipulaton
             {
                 public sealed class Node(string name) : TreeNode<Node>
                 {
                     public string Name { get; } = name;
                 }

                 [QmlElement]
                 [Qt.Export(Options = Qt.ExportAs.Metadata)]
                 public sealed class MetadataTree : TreeModel<Node>
                 {
                     public MetadataTree() => AddRoot(new Node("root"));
                 }

                 internal class Program
                 {
                     static int Main(string[] args)
                     {
                         Console.WriteLine("started");
                         return 0;
                     }
                 }
             }
         """;

        private static readonly string SourceCode = Source.Replace(
            "Qt.ExportAs.Metadata", "Qt.ExportAs.SourceCode");

        private static RunOptions RunOpts => new()
        {
            EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
            StdErr = Redirect.StdOut
        };

        [TestMethod]
        public async Task StartupRejectsAlteredDeployedBytes()
        {
            using var temp = new TempProject();
            temp.Create(new() { PackageReferences = [Packages.QtBridge] });
            temp.AddFile("Program.cs", Source);

            var build = await temp.BuildAsync(new()
            {
                Properties = [("QtBridgeMetadataFileName", MetadataFileName)]
            });
            temp.SaveLog();
            Assert.IsTrue(build.Ok, build.Output);

            var metadataPath = Path.Combine(temp.ExeDir, MetadataFileName);
            Assert.IsTrue(File.Exists(metadataPath),
                $"the build deployed no type metadata to checksum: {metadataPath}");

            var ok = await temp.RunAsync(RunOpts);
            Assert.AreEqual(0, ok.ExitCode, ok.StdOut);
            Assert.Contains("started", ok.StdOut);

            var metadata = await File.ReadAllBytesAsync(metadataPath, Token);
            var executable = await File.ReadAllBytesAsync(temp.ExePath, Token);

            await File.WriteAllBytesAsync(metadataPath, [.. metadata, (byte)' '], Token);
            var altered = await temp.RunAsync(RunOpts);
            Assert.AreNotEqual(0, altered.ExitCode, altered.StdOut);
            Assert.Contains("does not match the checksum", altered.StdOut);
            await File.WriteAllBytesAsync(metadataPath, metadata, Token);

            var manifest = IndexOf(executable, ManifestHeader);
            Assert.IsGreaterThanOrEqualTo(0, manifest, "no patched manifest header found");
            executable[manifest + ManifestChecksumOffset] ^= 0xff;
            await File.WriteAllBytesAsync(temp.ExePath, executable, Token);

            var corrupt = await temp.RunAsync(RunOpts);
            Assert.AreNotEqual(0, corrupt.ExitCode, corrupt.StdOut);
            // macOS rejects a modified signed Mach-O before it reaches main(), so it
            // cannot produce the native host's corruption diagnostic in that case.
            if (!OperatingSystem.IsMacOS() || !string.IsNullOrEmpty(corrupt.StdOut))
                Assert.Contains("corrupt", corrupt.StdOut);
            Assert.DoesNotContain("Unpatched", corrupt.StdOut);
        }

        [TestMethod]
        public async Task RebuildWithoutMetadataClearsHostManifest()
        {
            using var temp = new TempProject();
            temp.Create(new() { PackageReferences = [Packages.QtBridge] });
            temp.AddFile("Program.cs", Source);

            var buildOptions = new BuildOptions
            {
                Properties = [("QtBridgeMetadataFileName", MetadataFileName)]
            };
            var firstBuild = await temp.BuildAsync(buildOptions);
            Assert.IsTrue(firstBuild.Ok, firstBuild.Output);

            var metadataPath = Path.Combine(temp.ExeDir, MetadataFileName);
            Assert.IsTrue(File.Exists(metadataPath),
                $"the first build deployed no type metadata: {metadataPath}");

            temp.AddFile("Program.cs", SourceCode);
            var secondBuild = await temp.BuildAsync(buildOptions);
            temp.SaveLog();
            Assert.IsTrue(secondBuild.Ok, secondBuild.Output);
            Assert.IsFalse(File.Exists(metadataPath),
                $"the second build left stale type metadata: {metadataPath}");

            var executable = await File.ReadAllBytesAsync(temp.ExePath, Token);
            var manifest = IndexOf(executable, ManifestHeader);
            Assert.IsGreaterThanOrEqualTo(0, manifest, "no patched manifest header found");
            Assert.AreEqual(0, executable[manifest + MetadataNameOffset],
                "the host still contains a metadata file name");
            for (var i = 0; i < Sha256ChecksumSize; ++i) {
                Assert.AreEqual(0, executable[manifest + MetadataChecksumOffset + i],
                    $"metadata checksum byte {i} was not cleared");
            }
        }

        private static int IndexOf(byte[] source, byte[] pattern)
        {
            for (var i = 0; i <= source.Length - pattern.Length; ++i) {
                var match = true;
                for (var j = 0; match && j < pattern.Length; ++j)
                    match = source[i + j] == pattern[j];
                if (match)
                    return i;
            }
            return -1;
        }
    }
}
