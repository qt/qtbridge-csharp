// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.Build.Tasks;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    [TestClass]
    public class Test_QmlMetadataReader
    {
        public TestContext TestContext { get; set; }
        private CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;

        [TestMethod]
        public async Task FindMetadataFilePath_Finds_File_Under_Configuration_DirectoryA()
        {
            using var tempDir = new TempDir();
            var metadataFileDir = Directory.CreateDirectory(
                Path.Combine(tempDir.Path, "obj", "x64", "Debug"));
            await File.WriteAllTextAsync(Path.Combine(metadataFileDir.FullName,
                    QmlMetadataReader.MetadataFileName), "{}", CancellationToken);

            var reader = new QmlMetadataReader();
            var result = reader.FindMetadataFilePath(tempDir.Path, "Debug");

            Assert.IsNotNull(result);
            Assert.IsTrue(result.EndsWith(QmlMetadataReader.MetadataFileName,
                StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public async Task FindMetadataFilePath_Finds_File_By_Platform_Qualified_Key()
        {
            using var tempDir = new TempDir();
            var metadataFileDir = Directory.CreateDirectory(
                Path.Combine(tempDir.Path, "obj", "x64", "Debug"));
            await File.WriteAllTextAsync(Path.Combine(metadataFileDir.FullName,
                    QmlMetadataReader.MetadataFileName), "{}", CancellationToken);
            Directory.CreateDirectory(Path.Combine(tempDir.Path, "obj", "arm64", "Debug"));

            var reader = new QmlMetadataReader();
            var result = reader.FindMetadataFilePath(tempDir.Path,
                Path.Combine("x64", "Debug"));

            Assert.IsNotNull(result);
            Assert.IsTrue(result.EndsWith(QmlMetadataReader.MetadataFileName,
                StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public async Task FindMetadataFilePath_Returns_Null_When_Unqualified_Key_Is_Ambiguous()
        {
            using var tempDir = new TempDir();
            foreach (var platform in new[] { "x64", "arm64" }) {
                var dir = Directory.CreateDirectory(
                    Path.Combine(tempDir.Path, "obj", platform, "Debug"));
                await File.WriteAllTextAsync(Path.Combine(dir.FullName,
                        QmlMetadataReader.MetadataFileName), "{}", CancellationToken);
            }

            var reader = new QmlMetadataReader();
            var result = reader.FindMetadataFilePath(tempDir.Path, "Debug");

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task FindMetadataFilePath_Prefers_Exact_Bare_Path_Over_Platform_Qualified()
        {
            using var tempDir = new TempDir();
            var bareDir = Directory.CreateDirectory(Path.Combine(tempDir.Path, "obj", "Debug"));
            var expectedPath = Path.Combine(bareDir.FullName, QmlMetadataReader.MetadataFileName);
            await File.WriteAllTextAsync(expectedPath, "{}", CancellationToken);

            var staleDir = Directory.CreateDirectory(
                Path.Combine(tempDir.Path, "obj", "x64", "Debug"));
            await File.WriteAllTextAsync(
                Path.Combine(staleDir.FullName, QmlMetadataReader.MetadataFileName),
                "{}", CancellationToken);

            var reader = new QmlMetadataReader();
            var result = reader.FindMetadataFilePath(tempDir.Path, "Debug");
            Assert.AreEqual(expectedPath, result, StringComparer.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void FindMetadataFilePath_Returns_Null_When_No_Obj_Directory()
        {
            using var tempDir = new TempDir();

            var reader = new QmlMetadataReader();
            var result = reader.FindMetadataFilePath(tempDir.Path, "Debug");

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task FindMetadataFilePath_Returns_Null_For_Wrong_Configuration()
        {
            using var tempDir = new TempDir();
            var metadataFileDir = Directory.CreateDirectory(
                Path.Combine(tempDir.Path, "obj", "x64", "Debug"));
            await File.WriteAllTextAsync(
                Path.Combine(metadataFileDir.FullName, QmlMetadataReader.MetadataFileName),
                "{}", CancellationToken);

            var reader = new QmlMetadataReader();
            var result = reader.FindMetadataFilePath(tempDir.Path, "Release");
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TryRead_Deserializes_Valid_Metadata()
        {
            using var tempDir = new TempDir();
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var projectFilePath = Path.Combine(tempDir.Path, "App.csproj");
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            var dotQt = Path.Combine(buildDirPath, ".qt");
            var readyFilePath = Path.Combine(dotQt, BuildReadyMarker.FileName);
            var buildIniPath = Path.Combine(dotQt, QmllsBuildIniPatcher.FileName);
            var projectSourcesQrcPath = Path.Combine(dotQt, ProjectSourcesQrcWriter.FileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "{{projectFilePath.Replace("\\", @"\\")}}",
                  "configuration": "Debug",
                  "targetFramework": "net8.0",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "projectSourceDir": "{{tempDir.Path.Replace("\\", @"\\")}}",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  },
                  "qmlLanguageServer": {
                    "disableCMakeCalls": true,
                    "readyFile": "{{readyFilePath.Replace("\\", @"\\")}}",
                    "buildIni": "{{buildIniPath.Replace("\\", @"\\")}}",
                    "projectSourcesQrc": "{{projectSourcesQrcPath.Replace("\\", @"\\")}}"
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var result = readResult.Metadata;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Version);
            Assert.AreEqual(projectFilePath, result.ProjectFile);
            Assert.AreEqual("Debug", result.Configuration);
            Assert.AreEqual("net8.0", result.TargetFramework);
            Assert.AreEqual(sourceDirPath, result.Qml.SourceDir);
            Assert.AreEqual(tempDir.Path, result.Qml.ProjectSourceDir);
            Assert.HasCount(1, result.Qml.BuildDirs);
            Assert.AreEqual(buildDirPath, result.Qml.BuildDirs[0]);
            Assert.IsTrue(result.QmlLanguageServer.DisableCMakeCalls);
            Assert.AreEqual(readyFilePath, result.QmlLanguageServer.ReadyFile);
            Assert.AreEqual(buildIniPath, result.QmlLanguageServer.BuildIni);
            Assert.AreEqual(projectSourcesQrcPath, result.QmlLanguageServer.ProjectSourcesQrc);
        }

        [TestMethod]
        public async Task TryRead_DisableCMakeCalls_Defaults_To_True()
        {
            using var tempDir = new TempDir();
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "C:\\App\\App.csproj",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var result = readResult.Metadata;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.QmlLanguageServer.DisableCMakeCalls);
            Assert.IsNull(result.QmlLanguageServer.ReadyFile);
            Assert.IsNull(result.QmlLanguageServer.BuildIni);
            Assert.IsNull(result.QmlLanguageServer.ProjectSourcesQrc);
            Assert.IsNull(result.Qml.ProjectSourceDir);
            Assert.IsNull(result.TargetFramework);
        }

        [TestMethod]
        public async Task TryRead_Uses_Legacy_Qmlls_DisableCMakeCalls_When_Present()
        {
            using var tempDir = new TempDir();
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "C:\\App\\App.csproj",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  },
                  "qmlls": {
                    "disableCMakeCalls": false
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var result = readResult.Metadata;
            Assert.IsNotNull(result);
            Assert.IsFalse(result.QmlLanguageServer.DisableCMakeCalls);
        }

        [TestMethod]
        public void TryRead_Returns_Null_For_Missing_File()
        {
            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(
                $@"C:\does\not\exist\{QmlMetadataReader.MetadataFileName}",
                CancellationToken);
            Assert.IsFalse(readResult.Success);
            Assert.IsNull(readResult.Metadata);
        }

        [TestMethod]
        public async Task TryRead_Returns_Null_For_Missing_Required_Fields()
        {
            using var tempDir = new TempDir();
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, """
                {
                  "version": 1,
                  "projectFile": "C:\\App\\App.csproj"
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsFalse(readResult.Success);

            Assert.IsNull(readResult.Metadata);
        }

        [TestMethod]
        public async Task Validate_Passes_For_Valid_Metadata()
        {
            using var tempDir = new TempDir();
            var projectFilePath = Path.Combine(tempDir.Path, "App.csproj");
            await File.WriteAllTextAsync(projectFilePath, "", CancellationToken);
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "{{projectFilePath.Replace("\\", @"\\")}}",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var metadata = readResult.Metadata;
            Assert.IsNotNull(metadata);
            Assert.IsTrue(reader.Validate(metadata, projectFilePath, "Debug"));
        }

        [TestMethod]
        public async Task Validate_Fails_For_Wrong_Version()
        {
            using var tempDir = new TempDir();
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 99,
                  "projectFile": "C:\\App\\App.csproj",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var metadata = readResult.Metadata;
            Assert.IsNotNull(metadata);
            Assert.IsFalse(reader.Validate(metadata, @"C:\App\App.csproj", "Debug"));
        }

        [TestMethod]
        public async Task Validate_Fails_For_Wrong_Configuration()
        {
            using var tempDir = new TempDir();
            var projectFilePath = Path.Combine(tempDir.Path, "App.csproj");
            await File.WriteAllTextAsync(projectFilePath, "", CancellationToken);
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "{{projectFilePath.Replace("\\", @"\\")}}",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var metadata = readResult.Metadata;
            Assert.IsNotNull(metadata);
            Assert.IsFalse(reader.Validate(metadata, projectFilePath, "Release"));
        }

        [TestMethod]
        public async Task Validate_Fails_When_Any_BuildDir_Does_Not_Exist()
        {
            using var tempDir = new TempDir();
            var projectFilePath = Path.Combine(tempDir.Path, "App.csproj");
            await File.WriteAllTextAsync(projectFilePath, "", CancellationToken);
            var sourceDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "source"))
                .FullName;
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var missingBuildDirPath = Path.Combine(tempDir.Path, "missing-build");
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "{{projectFilePath.Replace("\\", @"\\")}}",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "{{sourceDirPath.Replace("\\", @"\\")}}",
                    "buildDirs": [
                      "{{buildDirPath.Replace("\\", @"\\")}}",
                      "{{missingBuildDirPath.Replace("\\", @"\\")}}"
                    ]
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var metadata = readResult.Metadata;
            Assert.IsNotNull(metadata);
            Assert.IsFalse(reader.Validate(metadata, projectFilePath, "Debug"));
        }

        [TestMethod]
        public async Task Validate_Fails_When_SourceDir_Does_Not_Exist()
        {
            using var tempDir = new TempDir();
            var projectFilePath = Path.Combine(tempDir.Path, "App.csproj");
            await File.WriteAllTextAsync(projectFilePath, "", CancellationToken);
            var buildDirPath = Directory.CreateDirectory(Path.Combine(tempDir.Path, "build"))
                .FullName;
            var metadataFilePath = Path.Combine(tempDir.Path, QmlMetadataReader.MetadataFileName);

            await File.WriteAllTextAsync(metadataFilePath, $$"""
                {
                  "version": 1,
                  "projectFile": "{{projectFilePath.Replace("\\", @"\\")}}",
                  "configuration": "Debug",
                  "qml": {
                    "sourceDir": "C:\\does\\not\\exist",
                    "buildDirs": ["{{buildDirPath.Replace("\\", @"\\")}}"]
                  }
                }
                """, CancellationToken);

            var reader = new QmlMetadataReader();
            var readResult = reader.TryRead(metadataFilePath, CancellationToken);
            Assert.IsTrue(readResult.Success);

            var metadata = readResult.Metadata;
            Assert.IsNotNull(metadata);
            Assert.IsFalse(reader.Validate(metadata, projectFilePath, "Debug"));
        }
    }
}
