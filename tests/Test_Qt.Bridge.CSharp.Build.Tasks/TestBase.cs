// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Security;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    public abstract class TestBase
    {
        internal static string RootDirectory { get; } =
            Path.Combine(Path.GetTempPath(), "qtbridge-build-tasks-tests");

        private string? tempDirectory;

        protected abstract string TempDirectoryName { get; }

        protected string TempDirectory
        {
            get
            {
                if (tempDirectory != null)
                    return tempDirectory;

                tempDirectory = Path.Combine(RootDirectory, TempDirectoryName,
                    Guid.NewGuid().ToString("N"));
                return tempDirectory;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (tempDirectory != null)
                TryDelete(tempDirectory);
        }

        // A killed process tree can lock the directory for a moment. If we leave something
        // behind, we'll remove it when the next test run starts.
        internal static void TryDelete(string directory)
        {
            for (var attempt = 0; Directory.Exists(directory); ++attempt) {
                try {
                    Directory.Delete(directory, recursive: true);
                } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
                    if (attempt >= 5)
                        return;
                    Thread.Sleep(500);
                }
            }
        }

        protected static string Normalize(string path) => path.Replace('\\', '/');

        protected static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null) {
                if (File.Exists(Path.Combine(directory.FullName, "build", "Qt.Bridge.targets")))
                    return directory.FullName;
                directory = directory.Parent;
            }
            throw new InvalidOperationException("Could not locate the repository root.");
        }

        protected static string XmlEscape(string? value) => SecurityElement.Escape(value) ?? "";
    }
}
