// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Text;
using System.Threading;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project
{
    using static Qt.Bridge.CSharp.Build.Tasks.PatchNativeHostManifest;

    [TestClass]
    public class Test_NativeHostPlaceholder : ManagedTestBase
    {
        public TestContext TestContext { get; set; }
        private CancellationTokenSource CTS => TestContext.CancellationTokenSource;

        // Magic value, format version 1, and payload length, all in little-endian format.
        // Consider the entire header and not just "QTBM" because the magic value also appears
        // as constant in the host which 'nativeHostAssemblyName()' compares against.
        private static readonly byte[] ManifestHeader =
            [(byte)'Q', (byte)'T', (byte)'B', (byte)'M', 1, 0,
                ManifestPayloadSize & 0xff,
                ManifestPayloadSize >> 8];

        [TestMethod]
        public async Task NativeHostPlaceholderIsPatched()
        {
            using var temp = new TempProject();
            var options = CreateQtQuickTestOptions(Path.Combine("QtQuickTest", "main.cpp"));
            await InitializeAndBuildAsync(temp, options, project => {
                project.CopyFile("Program.cs", Path.Combine("QtQuickTest", "Program.cs"));
                project.CopyFile("tst_qtquicktest.qml", Path
                    .Combine("QtQuickTest", "tst_qtquicktest.qml"));
            });

            var bytes = await File.ReadAllBytesAsync(temp.ExePath, CTS.Token);
            var executable = Encoding.ASCII.GetString(bytes);

            Assert.DoesNotContain(Marker, executable);
            Assert.DoesNotContain(SdkPlaceholder, executable);

            // The patched manifest header must exist, and only one copy of it.
            var manifest = IndexOf(bytes, ManifestHeader);
            Assert.IsGreaterThanOrEqualTo(0, manifest, "no patched manifest header found");
            Assert.AreEqual(-1, IndexOf(bytes, ManifestHeader, manifest + 1),
                "more than one patched manifest header found");

            // If the manifest is present, the assembly name must be present too.
            var expected = await temp.GetPropertyAsync("TargetFileName");
            Assert.IsFalse(string.IsNullOrEmpty(expected));
            Assert.AreEqual(expected, ReadNulTerminated(bytes, manifest + AssemblyNameOffset,
                AssemblyNameSize));
        }

        [TestMethod]
        public async Task NativeHostTemplateShipsUnpatched()
        {
            // The template the build starts from, resolved the way a real build would. No build
            // needed, QtAppHost comes from property evaluation.
            using var temp = new TempProject();
            temp.Create(CreateQtQuickTestOptions(Path.Combine("QtQuickTest", "main.cpp")));

            // Restore only: the property comes from evaluation, but MSBuild cannot evaluate the
            // project until its NuGet imports exist. The result is not asserted because BuildAsync
            // reports failure whenever no executable is produced, which a restore never does; the
            // property query below is the check.
            await temp.BuildAsync(new BuildOptions { Targets = ["Restore"] });

            var templatePath = await temp.GetPropertyAsync("QtAppHost");
            Assert.IsFalse(string.IsNullOrEmpty(templatePath), "QtAppHost did not resolve");
            Assert.IsTrue(File.Exists(templatePath), $"template not found: {templatePath}");

            var bytes = await File.ReadAllBytesAsync(templatePath, CTS.Token);
            var text = Encoding.ASCII.GetString(bytes);

            // Exactly one marker. A second copy would be patched by neither the build task nor
            // CreateAppHost, and would ship unpatched - the failure dotnet/runtime/issues/109611
            // describes for the SDK app host.
            var marker = text.IndexOf(Marker, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, marker, "the template carries no manifest marker");
            Assert.AreEqual(-1, text.IndexOf(Marker, marker + 1, StringComparison.Ordinal),
                "the template carries more than one manifest marker");

            // The SDK placeholder is injected at patch time, never compiled in, and it should not
            // be present in the template.
            Assert.DoesNotContain(SdkPlaceholder, text);

            // Nothing in the template may be zero. A zero tail is what lets the linker leave the
            // end of the region out of the file image, and the initialiser zero-fills in silence
            // if it is ever edited short of TemplateSize entries, so no compiler will warn about it.
            var zero = Array.IndexOf(bytes, (byte)0, marker, TemplateSize);
            Assert.AreEqual(-1, zero, $"zero byte at template offset {zero - marker}; the region "
                + " must be fully non-zero until the build patches it");
        }

        private static int IndexOf(byte[] source, byte[] pattern, int from = 0)
        {
            for (var i = from; i <= source.Length - pattern.Length; ++i) {
                var match = true;
                for (var j = 0; match && j < pattern.Length; ++j)
                    match = source[i + j] == pattern[j];
                if (match)
                    return i;
            }
            return -1;
        }

        private static string ReadNulTerminated(byte[] bytes, int offset, int maxLength)
        {
            var length = 0;
            while (length < maxLength && bytes[offset + length] != 0)
                ++length;
            return Encoding.UTF8.GetString(bytes, offset, length);
        }
    }
}
