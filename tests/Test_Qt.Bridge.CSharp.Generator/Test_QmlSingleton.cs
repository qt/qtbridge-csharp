// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.Bridge.CodeGeneration;
    using Qt.Quick;
    using Support;

    [TestClass]
    public class Test_QmlSingleton
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataRow(true, "pragma Singleton\nimport QtQuick\n\nQtObject { }\n")]
        [DataRow(true, "pragma\nSingleton\nimport QtQuick\n\nQtObject { }\n")]
        [DataRow(true, "pragma /*foo*/ Singleton\nimport QtQuick\n\nQtObject { }\n")]
        [DataRow(false, "//pragma Singleton\nimport QtQuick\n\nQtObject { }\n")]
        [DataRow(false, "/*\npragma Singleton\n*/\nimport QtQuick\n\nQtObject { }\n")]
        [DataRow(false, "#pragma Singleton\nimport QtQuick\n\nQtObject { }\n")]
        public async Task QmlDom_DetectSingleton(bool isSingleton, string qml)
        {
            var tempDirectory = Path
                .Combine(Path.GetTempPath(), "qtbridge qml " + Guid.NewGuid().ToString("N"));
            var tempQml = Path.Combine(tempDirectory, "Singleton.qml");
            var prevQtDir = Rules.QtDir;
            try {
                Directory.CreateDirectory(tempDirectory);
                await File.WriteAllTextAsync(tempQml, qml);
                Rules.QtDir = new DirectoryInfo(QtDir());
                var qmlDom = await Rule.QmlDom.ExecAsync(tempQml, "objects", "imports");
                Assert.AreEqual(isSingleton, qmlDom.IsSingleton);
            } finally {
                Rules.QtDir = prevQtDir;
                File.Delete(tempQml);
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory);
            }
        }

        [TestMethod]
        public async Task QmlDir_Singleton()
        {
            var singletonQml = Path
                .Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.qml");
            var nonSingletonQml = Path
                .Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.qml");
            try {
                await File.WriteAllTextAsync(singletonQml,
                    "pragma Singleton\nimport QtQuick\n\nQtObject { }\n");
                await File.WriteAllTextAsync(nonSingletonQml,
                    "//pragma Singleton\nimport QtQuick\n\nQtObject { }\n");
                var source = $$"""
                [assembly: Qt.Quick.QmlFile(
                    Uri = "App", TypeName = "Foo", IsRoot = true,
                    FilePath = @"{{singletonQml}}", Path = "App/Foo.qml")]
                [assembly: Qt.Quick.QmlFile(
                    Uri = "App", TypeName = "Bar", IsRoot = true,
                    FilePath = @"{{nonSingletonQml}}", Path = "App/Bar.qml")]

                public class Foo { public int Bar { get; set; } }
                """;

                using var result = await TestCodeGenerator.GenerateAsync([source],
                    sourceRefs: [typeof(QmlFileAttribute).Assembly],
                    qtDir: QtDir(), ct: TestContext.CancellationTokenSource.Token);
                result.SelectedFiles = ["source/qml/App/qmldir"];

                Assert.Contains("\nsingleton Foo ", result.Sink.Files["source/qml/App/qmldir"]);
                Assert.DoesNotContain("\nsingleton Bar ", result.Sink.Files["source/qml/App/qmldir"]);
            } finally {
                File.Delete(singletonQml);
                File.Delete(nonSingletonQml);
            }
        }

        [TestMethod]
        public async Task MalformedSingletonFailsGeneration()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.qml");
            try {
                await File.WriteAllTextAsync(path, "pragma Singleton\nimport QtQuick\nQtObject {");
                var source = $$"""
                [assembly: Qt.Quick.QmlFile(Uri="App", TypeName="Broken", IsRoot=true,
                    FilePath=@"{{path}}", Path="App/Broken.qml")]
                public class Foo { }
                """;
                var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                    TestCodeGenerator.GenerateAsync([source],
                        sourceRefs: [typeof(QmlFileAttribute).Assembly], qtDir: QtDir(),
                        ct: TestContext.CancellationTokenSource.Token));
                Assert.Contains("Failed to inspect QML file", error.Message);
                Assert.Contains(path, error.Message);
            } finally {
                File.Delete(path);
            }
        }

        private static string QtDir()
        {
            var path = Environment.GetEnvironmentVariable("QtDir")
                ?? Environment.GetEnvironmentVariable("QTDIR");
            Assert.IsFalse(string.IsNullOrWhiteSpace(path), "QtDir must be set for QML tests.");
            Assert.IsTrue(Directory.Exists(path), $"QtDir not found: {path}");
            return path;
        }
    }
}
