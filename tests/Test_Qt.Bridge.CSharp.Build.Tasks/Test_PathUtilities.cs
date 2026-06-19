// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_PathUtilities
    {
        [TestMethod]
        public void ToForwardSlashes_NormalizesBothHostStyles()
        {
            Assert.AreEqual("project/Views/Main.qml",
                PathUtilities.ToForwardSlashes(@"project\Views/Main.qml"));
        }

        [TestMethod]
        public void PathOperations_RejectNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => PathUtilities.ToForwardSlashes(null!));
            Assert.Throws<ArgumentNullException>(() => PathUtilities.ToHostSeparators(null!));
        }
    }
}
