// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Xml.Linq;
using Qt.Bridge.CSharp.Build.Tasks;

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public sealed class Test_ProjectSourcesQrcWriter : TestBase
    {
        private static readonly string[] ViewAliases = ["Details.qml", "List.qml"];
        private static readonly string[] ModulePrefixes =
            ["/qt/qml/Application", "/qt/qml/Views", "/qt/qml/views"];
        private static readonly string[] ApplicationAliases = ["A.qml", "B.qml"];

        [TestMethod]
        public void Write_GroupsFilesByModuleAndUsesFileNamesAsAliases()
        {
            var buildDirectory = CreateBuildDirectory();
            var files = new[]
            {
                CreateQmlFileInfo("Views/Details.qml", "Views", "Details"),
                CreateQmlFileInfo("Main.qml", "Application", "Main"),
                CreateQmlFileInfo("Views/List.qml", "Views", "List")
            };

            var result = ProjectSourcesQrcWriter.Write(buildDirectory, files);

            Assert.IsTrue(result.Changed);
            Assert.IsNotNull(result.Path);
            var document = XDocument.Load(result.Path);
            var resources = document.Root!.Elements("qresource").ToArray();
            Assert.HasCount(2, resources);
            Assert.AreEqual("/qt/qml/Application", (string?)resources[0].Attribute("prefix"));
            Assert.AreEqual("/qt/qml/Views", (string?)resources[1].Attribute("prefix"));
            CollectionAssert.AreEqual(
                ViewAliases,
                resources[1].Elements("file")
                    .Select(file => (string?)file.Attribute("alias"))
                    .ToArray());
        }

        [TestMethod]
        public void Write_PreservesCaseDistinctModuleIdentities()
        {
            var buildDirectory = CreateBuildDirectory();
            var files = new[]
            {
                CreateQmlFileInfo("z.qml", "views", "z"),
                CreateQmlFileInfo("B.qml", "Application", "B"),
                CreateQmlFileInfo("a.qml", "Views", "a"),
                CreateQmlFileInfo("A.qml", "Application", "A")
            };

            var result = ProjectSourcesQrcWriter.Write(buildDirectory, files);

            var document = XDocument.Load(result.Path!);
            CollectionAssert.AreEqual(
                ModulePrefixes,
                document.Root!.Elements("qresource")
                    .Select(resource => (string?)resource.Attribute("prefix"))
                    .ToArray());
            CollectionAssert.AreEqual(
                ApplicationAliases,
                document.Root.Elements("qresource").First().Elements("file")
                    .Select(file => (string?)file.Attribute("alias"))
                    .ToArray());
            Assert.AreEqual(
                "a.qml",
                (string?)document.Root.Elements("qresource").ElementAt(1)
                    .Elements("file").Single().Attribute("alias"));
            Assert.AreEqual(
                "z.qml",
                (string?)document.Root.Elements("qresource").ElementAt(2)
                    .Elements("file").Single().Attribute("alias"));
        }

        [TestMethod]
        public void Write_FiltersDuplicateResourceIdentitiesDeterministically()
        {
            var buildDirectory = Path.Combine(TempDirectory, "build");
            var firstSourcePath = Path.Combine(ProjectDirectory, "First", "View.qml");
            var secondSourcePath = Path.Combine(ProjectDirectory, "Second", "View.qml");

            Directory.CreateDirectory(Path.GetDirectoryName(firstSourcePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(secondSourcePath)!);
            File.WriteAllText(firstSourcePath, "");
            File.WriteAllText(secondSourcePath, "");

            var result = ProjectSourcesQrcWriter.Write(buildDirectory,
            [
                new QmlFileInfo(firstSourcePath, "Views", "FirstView"),
                new QmlFileInfo(secondSourcePath, @"Views\", "SecondView")
            ]);

            Assert.IsTrue(result.Changed);
            Assert.HasCount(1, result.Collisions);
            Assert.AreEqual("/qt/qml/Views/View.qml", result.Collisions.Single().ResourcePath);
            CollectionAssert.AreEqual(
                new[]
                {
                    firstSourcePath.Replace('\\', '/'),
                    secondSourcePath.Replace('\\', '/')
                },
                result.Collisions.Single().SourcePaths.ToArray());
            CollectionAssert.AreEqual(
                new[] { "View.qml" },
                XDocument.Load(result.Path!).Descendants("file")
                    .Select(file => (string?)file.Attribute("alias"))
                    .ToArray());
        }

        [TestMethod]
        public void Write_AllowsCaseDistinctResourceIdentities()
        {
            var buildDirectory = CreateBuildDirectory();
            var upperCasePath = Path.Combine(ProjectDirectory, "View.qml");
            var lowerCasePath = Path.Combine(ProjectDirectory, "view.qml");

            var result = ProjectSourcesQrcWriter.Write(buildDirectory,
            [
                new QmlFileInfo(upperCasePath, "Views", "View"),
                new QmlFileInfo(lowerCasePath, "Views", "view")
            ]);

            CollectionAssert.AreEqual(
                new[] { "View.qml", "view.qml" },
                XDocument.Load(result.Path!).Descendants("file")
                    .Select(file => (string?)file.Attribute("alias"))
                    .ToArray());
        }

        [TestMethod]
        public void Write_EscapesXmlAndNormalizesModuleSeparators()
        {
            var buildDirectory = CreateBuildDirectory();
            var sourcePath = Path.Combine(ProjectDirectory, "A&B.qml");
            File.WriteAllText(sourcePath, "");

            var result = ProjectSourcesQrcWriter.Write(
                buildDirectory,
                [new QmlFileInfo(sourcePath, @"Controls\A&B", "A&B")]);

            var content = File.ReadAllText(result.Path!);
            Assert.Contains("prefix=\"/qt/qml/Controls/A&amp;B\"", content, StringComparison.Ordinal);
            Assert.Contains("alias=\"A&amp;B.qml\"", content, StringComparison.Ordinal);
            Assert.AreEqual(
                "A&B.qml",
                (string?)XDocument.Load(result.Path!).Descendants("file").Single().Attribute("alias"));
        }

        [TestMethod]
        public void Write_UsesPathsRelativeToTheQrcDirectory()
        {
            var buildDirectory = CreateBuildDirectory();
            var sourcePath = Path.Combine(ProjectDirectory, "Main.qml");
            File.WriteAllText(sourcePath, "");

            var result = ProjectSourcesQrcWriter.Write(
                buildDirectory,
                [new QmlFileInfo(sourcePath, "Application", "Main")]);

            var filePath = XDocument.Load(result.Path!).Descendants("file").Single().Value;
            var expected = new Uri(AppendDirectorySeparator(Path.GetDirectoryName(result.Path!)!))
                .MakeRelativeUri(new Uri(sourcePath))
                .ToString();
            Assert.AreEqual(Uri.UnescapeDataString(expected).Replace('\\', '/'), filePath);
        }

        [TestMethod]
        public void Write_ResolvesSourcePathsWithNonNativeSeparators()
        {
            var buildDirectory = CreateBuildDirectory();
            var sourcePath = Path.Combine(ProjectDirectory, "Views", "Details.qml");
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllText(sourcePath, "");
            var nonNativePath = Path.DirectorySeparatorChar == '/'
                ? sourcePath.Replace('/', '\\')
                : sourcePath.Replace('\\', '/');

            var result = ProjectSourcesQrcWriter.Write(
                buildDirectory,
                [new QmlFileInfo(nonNativePath, @"Views\Details", "Details")]);

            var file = XDocument.Load(result.Path!).Descendants("file").Single();
            Assert.AreEqual("Details.qml", (string?)file.Attribute("alias"));
            Assert.AreEqual("/qt/qml/Views/Details", (string?)file.Parent!.Attribute("prefix"));
            Assert.DoesNotContain("\\", file.Value, StringComparison.Ordinal);
            Assert.EndsWith("/Views/Details.qml", file.Value, StringComparison.Ordinal);
        }

        [TestMethod]
        public void Write_DoesNotRewriteUnchangedContent()
        {
            var buildDirectory = CreateBuildDirectory();
            var files = new[] { CreateQmlFileInfo("Main.qml", "Application", "Main") };
            var first = ProjectSourcesQrcWriter.Write(buildDirectory, files);
            File.SetLastWriteTimeUtc(first.Path!, DateTime.UtcNow.AddMinutes(-5));
            var timestamp = File.GetLastWriteTimeUtc(first.Path!);
            var second = ProjectSourcesQrcWriter.Write(buildDirectory, files);

            Assert.IsFalse(second.Changed);
            Assert.AreEqual(first.Path, second.Path);
            Assert.AreEqual(timestamp, File.GetLastWriteTimeUtc(second.Path!));
        }

        [TestMethod]
        public void Write_RewritesChangedContent()
        {
            var buildDirectory = CreateBuildDirectory();
            var first = ProjectSourcesQrcWriter.Write(
                buildDirectory,
                [CreateQmlFileInfo("Main.qml", "Application", "Main")]);

            var second = ProjectSourcesQrcWriter.Write(
                buildDirectory,
                [
                    CreateQmlFileInfo("Main.qml", "Application", "Main"),
                    CreateQmlFileInfo("About.qml", "Application", "About")
                ]);

            Assert.IsTrue(first.Changed);
            Assert.IsTrue(second.Changed);
            Assert.HasCount(2, XDocument.Load(second.Path!).Descendants("file"));
        }

        [TestMethod]
        public void Write_ReturnsNoPathAndCreatesNoDirectoryForEmptyInput()
        {
            var buildDirectory = Path.Combine(TempDirectory, "build");

            var result = ProjectSourcesQrcWriter.Write(buildDirectory, []);

            Assert.IsNull(result.Path);
            Assert.IsFalse(result.Changed);
            Assert.IsFalse(Directory.Exists(buildDirectory));
        }

        [TestMethod]
        public void Write_RemovesStaleQrcForEmptyInput()
        {
            var buildDirectory = CreateBuildDirectory();
            var first = ProjectSourcesQrcWriter.Write(
                buildDirectory,
                [CreateQmlFileInfo("Main.qml", "Application", "Main")]);

            var result = ProjectSourcesQrcWriter.Write(buildDirectory, []);

            Assert.IsNull(result.Path);
            Assert.IsTrue(result.Changed);
            Assert.IsFalse(File.Exists(first.Path));
        }

        protected override string TempDirectoryName => "qtbridge-build-task-tests";

        private string ProjectDirectory
        {
            get
            {
                var projectDirectory = Path.Combine(TempDirectory, "project");
                Directory.CreateDirectory(projectDirectory);
                return projectDirectory;
            }
        }

        private string CreateBuildDirectory() =>
            Directory.CreateDirectory(Path.Combine(TempDirectory, "build")).FullName;

        private QmlFileInfo CreateQmlFileInfo(string relativePath, string modulePath, string typeName)
        {
            var sourcePath = Path.Combine(ProjectDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllText(sourcePath, "");
            return new QmlFileInfo(sourcePath, modulePath, typeName);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
