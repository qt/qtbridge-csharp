// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    public abstract class TestBase
    {
        private string? tempDirectory;

        protected abstract string TempDirectoryName { get; }

        protected string TempDirectory
        {
            get
            {
                if (tempDirectory != null)
                    return tempDirectory;

                tempDirectory = Path.Combine(Path.GetTempPath(), TempDirectoryName,
                    Guid.NewGuid().ToString("N"));
                return tempDirectory;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (tempDirectory != null && Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
