// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.Utils;

namespace Test_Utils
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
        public void AreEquivalent_TreatsWindowsPathsCaseInsensitively()
        {
            Assert.IsTrue(PathUtilities.AreEquivalent(@"C:\Work\Project\", "c:/work/project"));
        }

        [TestMethod]
        public void AreEquivalent_TreatsUnixPathsCaseSensitively()
        {
            Assert.IsFalse(PathUtilities.AreEquivalent("/work/Project", "/work/project"));
        }

        [TestMethod]
        public void AreEquivalent_AcceptsMixedSeparators()
        {
            Assert.IsTrue(PathUtilities.AreEquivalent(@"C:\work/project", "C:/work/project/"));
        }

        [TestMethod]
        public void AreEquivalent_DistinguishesDriveRootFromDriveRelativePath()
        {
            Assert.IsFalse(PathUtilities.AreEquivalent("C:/", "C:"));
        }

        [TestMethod]
        public void AreEquivalent_PreservesUnixRoot()
        {
            Assert.IsTrue(PathUtilities.AreEquivalent("/", "/./"));
        }

        [TestMethod]
        public void AreEquivalent_HandlesDriveRelativePathsCaseInsensitively()
        {
            Assert.IsTrue(PathUtilities.AreEquivalent(@"C:Project\View.qml", "c:project/view.qml"));
        }

        [TestMethod]
        public void AreEquivalent_HandlesUncPathsCaseInsensitively()
        {
            Assert.IsTrue(
                PathUtilities.AreEquivalent(@"\\Server\Share\Project\",
                "//server/share/project"));
        }

        [TestMethod]
        public void AreEquivalent_HandlesExtendedWindowsPathsCaseInsensitively()
        {
            Assert.IsTrue(
                PathUtilities.AreEquivalent(@"\\?\C:\Work\Project\",
                "//?/c:/work/project"));
            Assert.IsFalse(PathUtilities.AreEquivalent(@"\\?\C:\", @"\\?\C:"));
        }

        [TestMethod]
        public void PathOperations_RejectNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => PathUtilities.ToForwardSlashes(null!));
            Assert.Throws<ArgumentNullException>(() => PathUtilities.ToHostSeparators(null!));
            Assert.Throws<ArgumentNullException>(() => PathUtilities.AreEquivalent(null!, "path"));
            Assert.Throws<ArgumentNullException>(() => PathUtilities.AreEquivalent("path", null!));
            Assert.Throws<ArgumentNullException>(() => PathUtilities.IsCaseInsensitive(null!));
        }
    }
}
