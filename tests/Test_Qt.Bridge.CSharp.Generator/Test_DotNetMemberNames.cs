// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.Bridge;
    using Support;

    [TestClass]
    public class Test_DotNetMemberNames
    {
        public TestContext TestContext { get; set; }

        private const string Source = """
            using System;
            using System.ComponentModel;
            using Qt;

            namespace Test
            {
                public class Members : INotifyPropertyChanged
                {
                    public event PropertyChangedEventHandler PropertyChanged;
                    public event EventHandler _Reloaded;
                    public int _Count { get; set; }
                    public void _Reset() { }
                }
            }
            """;

        // The generated code accesses .NET members by name; therefore, the names must be used
        // exactly as declared in C# - even if the name intended for QML was modified to ensure
        // compatibility with C++.
        [TestMethod]
        public async Task SourceCode_UsesDeclaredDotNetNames()
        {
            using var result = await Generate(Qt.ExportAs.SourceCode);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/test/members.cpp", out var cpp),
                string.Join(", ", result.Sink.Files.Keys));
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/members.h", out var hpp));
            var code = hpp + cpp;

            Assert.Contains("method(\"_Reset\"", code);
            Assert.Contains("propertyName == \"_Count\"", code);
            Assert.Contains("Q_DOTNET_EVENT_HANDLER(_Reloaded,", code);
            Assert.DoesNotContain("z_Reset\"", code);
            Assert.DoesNotContain("\"z_Count\"", code);
        }

        [TestMethod]
        public async Task Metadata_UsesDeclaredDotNetNames()
        {
            using var result = await Generate(Qt.ExportAs.Metadata);

            Assert.IsTrue(result.Sink.Files.TryGetValue(TestCodeGenerator.MetadataFileName,
                out var json), string.Join(", ", result.Sink.Files.Keys));
            Assert.IsTrue(Metadata.FromJson(json).TypesByName.TryGetValue("Test.Members",
                out var members));

            Assert.IsTrue(members.PropertiesByName.ContainsKey("_Count"));
            Assert.IsTrue(members.MethodsByName.ContainsKey("_Reset"));
            CollectionAssert.Contains(members.Events.Select(e => e.DotNet.Name).ToArray(),
                "_Reloaded");
        }

        // Parameters are passed to .NET by position rather than by name, their names serve
        // just as C++ identifiers and must therefore, unlike element names, be valid C++.
        [TestMethod]
        public async Task SourceCode_SanitizesParameterNames()
        {
            const string source = """
                namespace Test
                {
                    public class Parameters
                    {
                        public int Scale(int _factor, int constexpr) => _factor * constexpr;
                    }
                }
                """;

            using var result = await TestCodeGenerator.GenerateAsync([source],
                sourceRefs: [typeof(Qt.ExportAttribute).Assembly],
                defaultExportOptions: Qt.ExportAs.SourceCode,
                ct: TestContext.CancellationTokenSource.Token);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/parameters.h", out var hpp),
                string.Join(", ", result.Sink.Files.Keys));
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/test/parameters.cpp",
                out var cpp));

            Assert.Contains("Q_INVOKABLE qint32 scale(qint32 z_factor, qint32 constexpr_) const;",
                hpp);
            Assert.Contains("Test::Parameters::scale(qint32 z_factor, qint32 constexpr_) const",
                cpp);
            Assert.Contains("method(\"Scale\"", cpp);
            Assert.Contains(".invoke(*this, z_factor, constexpr_)", cpp);
        }

        private Task<TestCodeGenerator.Result> Generate(Qt.Options exportAs)
        {
            return TestCodeGenerator.GenerateAsync([Source],
                sourceRefs:
                [
                    typeof(Qt.ExportAttribute).Assembly,
                    typeof(System.ComponentModel.INotifyPropertyChanged).Assembly,
                    typeof(Qt.Bridge.CodeGeneration.Rules.Metadata.GenerateProperty).Assembly
                ],
                defaultExportOptions: exportAs,
                ct: TestContext.CancellationTokenSource.Token);
        }
    }
}
