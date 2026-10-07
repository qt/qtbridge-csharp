// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public class Test_ExitCodeHelper
    {
        [TestMethod]
        [DataRow(0, "")]
        [DataRow(1, "")]
        [DataRow(2, "2 test functions failed")]
        [DataRow(127, "127 test functions failed")]
        [DataRow(101, "Failed to locate the generated .NET test assembly")]
        [DataRow(-1073741819, "Crashed with exception code 0xC0000005")]
        [DataRow(139, "Terminated by signal 11")]
        [DataRow(200, "Failed with unknown error, exit(200)")]
        public void ToString_DescribesExitCode(int exitCode, string expected)
        {
            Assert.AreEqual(expected, ExitCodeHelper.ToString(exitCode));
        }
    }
}
