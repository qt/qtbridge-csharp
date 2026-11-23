// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.IO;

namespace Test_Qt.Bridge.Project.Shared
{
    [TestClass]
    public class Test_TempProjectCleanup
    {
        [TestMethod]
        public void HeldFile_LeavesDirectoryBehind_WithoutThrowing()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            try {
                // Only Windows refuses to delete a file that is open.
                if (OperatingSystem.IsWindows()) {
                    using var held = new FileStream(Path.Combine(dir, "app.exe"),
                        FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    Assert.IsFalse(TempProject.DeleteDirectory(dir, timeoutMs: 300));
                    Assert.IsTrue(Directory.Exists(dir));
                }
                Assert.IsTrue(TempProject.DeleteDirectory(dir));
                Assert.IsFalse(Directory.Exists(dir));
            } finally {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }
}
