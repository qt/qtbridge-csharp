// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.Utils;

namespace Test_Utils
{
    using static Test_FileSystemInfoExtensions.PathResult;
    using static Test_FileSystemInfoExtensions.TestCondition;
    using static Test_FileSystemInfoExtensions.SubPathResult;

    [TestClass]
    public class Test_FileSystemInfoExtensions
    {
        [Flags]
        public enum PathResult
        {
            NoMatch = 0,
            Match = 1
        }

        public enum SubPathResult
        {
            NoSubPath = 0,
            SubPath = 1
        }

        [Flags]
        public enum TestCondition
        {
            None = 0,
            If_BackslashIsDirSeparator = 1 << 0,
            If_PathsAreCaseInsensitive = 1 << 1
        }

        private static bool BackslashIsDirSeparator
            => Path.DirectorySeparatorChar == '\\' || Path.AltDirectorySeparatorChar == '\\';

        private static bool PathsAreCaseInsensitive => Path.GetRelativePath("a", "A") == ".";

        private static bool ExpectedResult(bool expected, TestCondition condition)
        {
            if (!expected)
                return false;
            if (condition.HasFlag(If_BackslashIsDirSeparator))
                expected &= BackslashIsDirSeparator;
            if (condition.HasFlag(If_PathsAreCaseInsensitive))
                expected &= PathsAreCaseInsensitive;
            return expected;
        }

        private static DirectoryInfo? Directory(string path)
            => string.IsNullOrWhiteSpace(path) ? null : new DirectoryInfo(path);

        private static FileInfo? File(string path)
            => string.IsNullOrWhiteSpace(path) ? null : new FileInfo(path);

        [TestMethod]
        [DataRow(null, null, NoMatch, None)]
        [DataRow("", " ", NoMatch, None)]
        [DataRow("", null, NoMatch, None)]
        [DataRow("x/y", null, NoMatch, None)]
        [DataRow(null, "x/y", NoMatch, None)]
        [DataRow("x/y/z", "x/y/z", Match, None)]
        [DataRow("x/y/z", "x/y/z/", Match, None)]
        [DataRow("x/y/z", "x\\y\\z", Match, If_BackslashIsDirSeparator)]
        [DataRow("x/y/z", "x/Y/z", Match, If_PathsAreCaseInsensitive)]
        [DataRow("x/y", "x\\Y", Match, If_BackslashIsDirSeparator | If_PathsAreCaseInsensitive)]
        [DataRow("x/y", "x", NoMatch, None)]
        public void PathEquals(string left, string right, PathResult result, TestCondition condition)
        {
            var expected = ExpectedResult(result == Match, condition);
            Assert.AreEqual(expected, Directory(left).PathEquals(Directory(right)));
            Assert.AreEqual(expected, File(left).PathEquals(File(right)));
        }

        [TestMethod]
        [DataRow(null, null, NoSubPath, None)]
        [DataRow(null, "x/y", NoSubPath, None)]
        [DataRow("x/y", null, NoSubPath, None)]
        [DataRow("x/y/z", "x/y/z", NoSubPath, None)]
        [DataRow("x/y/z", "x/y", SubPath, None)]
        [DataRow("x/y/z", "x\\y", SubPath, If_BackslashIsDirSeparator)]
        [DataRow("x/y/z", "x/Y", SubPath, If_PathsAreCaseInsensitive)]
        [DataRow("x/y/z", "x\\Y", SubPath, If_BackslashIsDirSeparator | If_PathsAreCaseInsensitive)]
        [DataRow("x/y/z", "a/b", NoSubPath, None)]
        public void IsSubPathOf(string child,
            string parent, SubPathResult result, TestCondition condition)
        {
            var expected = ExpectedResult(result == SubPath, condition);
            Assert.AreEqual(expected, Directory(child).IsSubPathOf(Directory(parent)));
            Assert.AreEqual(expected, File(child).IsSubPathOf(Directory(parent)));
        }

        [TestMethod]
        public void FileInsideDescendantIsDescendant()
        {
            Assert.IsTrue(new FileInfo("x/y/z/file").IsSubPathOf(new DirectoryInfo("x/y")));
        }
    }
}
