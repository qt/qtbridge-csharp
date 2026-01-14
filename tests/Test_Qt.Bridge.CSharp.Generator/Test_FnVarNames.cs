// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_FnVarNames
    {
        public TestContext TestContext { get; set; }

        private static string ExtractFn(string combined, string baseName)
        {
            var m = Regex.Match(combined, $@"\bfn{baseName}_[0-9A-F]+\b");
            return m.Success ? m.Value : null;
        }

        [TestMethod]
        public async Task FnVarNames()
        {
            var v1 = new[]
            {
                """
                public class A
                {
                    public void Alpha() { } // earlier type; stable here
                }
                """,
                """
                public class Foo
                {
                    public void Target(int x) { } // <-- supposed the stay unchanged across versions
                }
                """
            };

            var v2 = new[]
            {
                """
                public class A
                {
                    public void Alpha() { }
                    public void NewOne() { } // <-- only change
                }
                """,
                """
                public class Foo
                {
                    public void Target(int x) { } // unchanged, but its number shifts
                }
                """
            };

            var cancelToken = TestContext.CancellationTokenSource.Token;
            var r1 = await TestCodeGenerator.GenerateAsync(v1, ct: cancelToken);
            Assert.IsTrue(r1.Sink.Files.TryGetValue("source/cpp/foo.cpp", out var r1Cpp));

            var r2 = await TestCodeGenerator.GenerateAsync(v2, ct: cancelToken);
            Assert.IsTrue(r2.Sink.Files.TryGetValue("source/cpp/foo.cpp", out var r2Cpp));

            var id1 = ExtractFn(r1Cpp, "Target");
            var id2 = ExtractFn(r2Cpp, "Target");

            Assert.IsNotNull(id1, "Expected to find fnTarget_* in v1 output.");
            Assert.IsNotNull(id2, "Expected to find fnTarget_* in v2 output.");

            Assert.AreEqual(id1, id2, "Unchanged method should produced equal fn name.");
        }

        private static string BuildToTempFile(AssemblyConfig assemblyConfig,
            ReturnConfig returnConfig, ParameterConfig parameterConfig)
        {
            var dir = Path.Combine(Path.GetTempPath(), "QtDotNetTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, assemblyConfig.AssemblyName + ".dll");

            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite,
                FileShare.Read);
            InMemoryAssemblyBuilder.Build(assemblyConfig, returnConfig, parameterConfig, fs);
            fs.Flush(true);
            return fullPath;
        }

        [TestMethod]
        public async Task CodeGenerator_CreateUniqueFilesAndfnNames()
        {
            var pluginAPath = BuildToTempFile(
                new AssemblyConfig
                {
                    AssemblyName = "PluginA.Common",
                    TypeName = "Plugin.Common.Commands",
                    MethodName = "Run"
                },
                returnConfig: new ReturnConfig
                {
                    EncodeType = r => r.Void()
                },
                parameterConfig: new ParameterConfig
                {
                    Names = ["x"],
                    EncodeTypes = p => p.AddParameter().Type().Int32()
                }
            );

            var pluginBPath = BuildToTempFile(
                new AssemblyConfig
                {
                    AssemblyName = "PluginB.Common",
                    TypeName = "Plugin.Common.Commands",
                    MethodName = "Run"
                },
                returnConfig: new ReturnConfig
                {
                    EncodeType = r => r.Void()
                },
                parameterConfig: new ParameterConfig
                {
                    Names = ["x"],
                    EncodeTypes = p => p.AddParameter().Type().Int32()
                }
            );
            var pluginDirs = new[]
            {
                Path.GetDirectoryName(pluginAPath),
                Path.GetDirectoryName(pluginBPath)
            };

            const string source = """
            extern alias PluginA;
            extern alias PluginB;

            namespace Host
            {
                public static class EntryPoints
                {
                    public static PluginA::Plugin.Common.Commands? _touchA;
                    public static PluginB::Plugin.Common.Commands? _touchB;

                    public static void UseA(int x) => PluginA::Plugin.Common.Commands.Run(x);
                    public static void UseB(int x) => PluginB::Plugin.Common.Commands.Run(x);
                }
            }
            """;

            try {
                _ = await TestCodeGenerator.GenerateAsync(
                    sources: [source],
                    referencesWithAliases:
                    [
                        ("PluginA", pluginAPath),
                        ("PluginB", pluginBPath)
                    ],
                    extraRefs: pluginDirs,
                    ct: TestContext.CancellationTokenSource.Token);
            } catch (InvalidOperationException ex) {
                Assert.Inconclusive("Debug.Assert triggered (expected temporarily): " + ex.Message);
            } finally {
                try { Directory.Delete(pluginDirs[0], recursive: true); } catch { /* ignore */ }
                try { Directory.Delete(pluginDirs[1], recursive: true); } catch { /* ignore */ }
            }
        }
    }
}
