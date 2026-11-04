// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Extension
{
    [TestClass]
    public sealed class Test_LoggingOptions
    {
        // Illegal path characters make Path.GetFullPath throw on .NET Framework.
        private const string UnusableDirectory = "C:\\qtbridge<invalid>|dir";

        [TestMethod]
        public void Log_Without_Directory_Is_Off_And_Does_Not_Affect_The_Other_Log()
        {
            var directory = CreateTempDirectory();
            try {
                var options = LoggingOptions.Create(
                    qmllsEnabled: true, qmllsDirectory: "",
                    lspEnabled: true, lspDirectory: directory);

                Assert.AreEqual("", options.QmllsLogFilePath);
                Assert.IsFalse(options.QmllsLogActive);
                Assert.AreEqual(Path.Combine(directory, "qtbridge-lsp.log"),
                    options.LspLogFilePath);
                Assert.IsTrue(options.LspLogActive);
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        [TestMethod]
        public void Unusable_Directory_Turns_Only_That_Log_Off()
        {
            var directory = CreateTempDirectory();
            try {
                var options = LoggingOptions.Create(
                    qmllsEnabled: true, qmllsDirectory: directory,
                    lspEnabled: true, lspDirectory: UnusableDirectory);

                Assert.AreEqual(Path.Combine(directory, "qtbridge-qmlls.log"),
                    options.QmllsLogFilePath);
                Assert.IsTrue(options.QmllsLogActive);
                Assert.AreEqual("", options.LspLogFilePath);
                Assert.IsFalse(options.LspLogActive);
            } finally {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static string CreateTempDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qtbridge-logging-options-tests",
                Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
