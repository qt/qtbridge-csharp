// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.Utils;

namespace Test_Utils
{
    using static Test_FileSystemInfoExtensions.PathResult;
    using static Test_FileSystemInfoExtensions.PathCaseSensitivity;
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

        public enum PathCaseSensitivity
        {
            CaseIndependent,
            RequiresCaseInsensitivePaths
        }

        private static bool PathsAreCaseInsensitive => Path.GetRelativePath("a", "A") == ".";

        private static bool ExpectedResult(bool expected, PathCaseSensitivity caseSensitivity) =>
            expected && (caseSensitivity != RequiresCaseInsensitivePaths || PathsAreCaseInsensitive);

        private static DirectoryInfo? Directory(string path)
            => string.IsNullOrWhiteSpace(path) ? null : new DirectoryInfo(path);

        private static FileInfo? File(string path)
            => string.IsNullOrWhiteSpace(path) ? null : new FileInfo(path);

        [TestMethod]
        [DataRow(null, null, NoMatch, CaseIndependent)]
        [DataRow("", " ", NoMatch, CaseIndependent)]
        [DataRow("", null, NoMatch, CaseIndependent)]
        [DataRow("x/y", null, NoMatch, CaseIndependent)]
        [DataRow(null, "x/y", NoMatch, CaseIndependent)]
        [DataRow("x/y/z", "x/y/z", Match, CaseIndependent)]
        [DataRow("x/y/z", "x/y/z/", Match, CaseIndependent)]
        [DataRow("x/y/z", "x\\y\\z", Match, CaseIndependent)]
        [DataRow("x/y/z", "x/Y/z", Match, RequiresCaseInsensitivePaths)]
        [DataRow("x/y", "x\\Y", Match, RequiresCaseInsensitivePaths)]
        [DataRow("x/y", "x", NoMatch, CaseIndependent)]
        public void PathEquals(string left, string right, PathResult result,
            PathCaseSensitivity caseSensitivity)
        {
            var expected = ExpectedResult(result == Match, caseSensitivity);
            Assert.AreEqual(expected, Directory(left).PathEquals(Directory(right)));
            Assert.AreEqual(expected, File(left).PathEquals(File(right)));
        }

        [TestMethod]
        [DataRow(null, null, NoSubPath, CaseIndependent)]
        [DataRow(null, "x/y", NoSubPath, CaseIndependent)]
        [DataRow("x/y", null, NoSubPath, CaseIndependent)]
        [DataRow("x/y/z", "x/y/z", NoSubPath, CaseIndependent)]
        [DataRow("x/y/z", "x/y", SubPath, CaseIndependent)]
        [DataRow("x/y/z", "x\\y", SubPath, CaseIndependent)]
        [DataRow("x/y/z", "x/Y", SubPath, RequiresCaseInsensitivePaths)]
        [DataRow("x/y/z", "x\\Y", SubPath, RequiresCaseInsensitivePaths)]
        [DataRow("x/y/z", "a/b", NoSubPath, CaseIndependent)]
        public void IsSubPathOf(string child,
            string parent, SubPathResult result, PathCaseSensitivity caseSensitivity)
        {
            var expected = ExpectedResult(result == SubPath, caseSensitivity);
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
