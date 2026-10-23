// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Moq;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    [TestClass]
    public class Test_QtBridgeProjectService
    {
        public TestContext TestContext { get; set; }

        private CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;

        // Stands in for the Visual Studio project system.
        private static Mock<IQtBridgeProjectCapabilityDetector> MockCapability(
            bool hasCapability,
            string? packageId = null)
        {
            var mock = new Mock<IQtBridgeProjectCapabilityDetector>();
            mock.Setup(detector => detector.EvaluateAsync(
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvaluationResult(hasCapability, packageId));
            return mock;
        }

        private static QtBridgeProjectService CreateService(
            IQtBridgeProjectCapabilityDetector capabilityDetector)
        {
            return new QtBridgeProjectService(new QtBridgeProjectFileLocator(),
                new QtBridgeProjectStaticDetector(), capabilityDetector);
        }

        private async Task<string> WriteFileAsync(TempDir tempDir, string name, string contents)
        {
            var path = Path.Combine(tempDir.Path, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, contents, CancellationToken);
            return path;
        }

        private const string QtBridgePackageReference =
            """
            <Project>
              <ItemGroup>
                <PackageReference Include="QtGroup.Qt.Bridge.CSharp.linux-x64" Version="0.5.*" />
              </ItemGroup>
            </Project>
            """;

        private const string PlainProject =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        private async Task AssertDetectedOnlyByCapabilityAsync(string projectPath)
        {
            var staticMetadata = await new QtBridgeProjectStaticDetector()
                .DetectAsync(projectPath, CancellationToken);
            Assert.IsFalse(staticMetadata.IsQtBridgeProject,
                "The project file analysis is not expected to evaluate MSBuild.");

            var capability = MockCapability(hasCapability: true);
            var metadata = await CreateService(capability.Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsTrue(metadata.HasQtBridgeCapability);
            Assert.AreEqual(QtBridgeProjectType.QtBridgeCSharp, metadata.ProjectType);
            capability.Verify(detector => detector.EvaluateAsync(
                Path.GetFullPath(projectPath), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Detects_Project_By_Capability_When_Static_Analysis_Misses()
        {
            using var tempDir = new TempDir();
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj", PlainProject);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Project_With_UserDefinedProperty_By_Capability()
        {
            using var tempDir = new TempDir();
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <MyProperty>QtGroup.Qt.Bridge.CSharp.linux-x64</MyProperty>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="$(MyProperty)" Version="0.4.*-*" />
                  </ItemGroup>
                </Project>
                """);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Project_With_ImportedPackageRefs_By_Capability()
        {
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Packages.props",
                """
                <Project>
                  <ItemGroup>
                    <PackageReference
                      Include="QtGroup.Qt.Bridge.CSharp.linux-x64" Version="0.4.*-*" />
                  </ItemGroup>
                </Project>
                """);
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <Import Project="$(MSBuildThisFileDirectory)Packages.props"/>
                </Project>
                """);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Project_With_PackageRef_In_DirectoryBuildProps_By_Capability()
        {
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Directory.Build.props", QtBridgePackageReference);
            var projectPath = await WriteFileAsync(tempDir, Path.Combine("App", "App.csproj"),
                PlainProject);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Project_With_PackageRef_In_DirectoryBuildTargets_By_Capability()
        {
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Directory.Build.targets", QtBridgePackageReference);
            var projectPath = await WriteFileAsync(tempDir, Path.Combine("App", "App.csproj"),
                PlainProject);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Project_With_GlobalPackageReference_By_Capability()
        {
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Directory.Packages.props",
                """
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <GlobalPackageReference Include="QtGroup.Qt.Bridge.CSharp.linux-x64"
                      Version="0.5.0" />
                  </ItemGroup>
                </Project>
                """);
            var projectPath = await WriteFileAsync(tempDir, Path.Combine("App", "App.csproj"),
                PlainProject);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Project_With_PackageId_From_Imported_Property_By_Capability()
        {
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "QtBridge.props",
                """
                <Project>
                  <PropertyGroup>
                    <BridgePackage>QtGroup.Qt.Bridge.CSharp.linux-x64</BridgePackage>
                  </PropertyGroup>
                </Project>
                """);
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <Import Project="QtBridge.props" />
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="$(BridgePackage)" Version="0.5.*" />
                  </ItemGroup>
                </Project>
                """);

            await AssertDetectedOnlyByCapabilityAsync(projectPath);
        }

        [TestMethod]
        public async Task Detects_Central_Package_Management_Project_Statically()
        {
            // With central package management the project file still has the package ID, only
            // the version is in Directory.Packages.props.
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Directory.Packages.props",
                """
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageVersion Include="QtGroup.Qt.Bridge.CSharp.linux-x64" Version="0.5.0" />
                  </ItemGroup>
                </Project>
                """);
            var projectPath = await WriteFileAsync(tempDir, Path.Combine("App", "App.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="QtGroup.Qt.Bridge.CSharp.linux-x64" />
                  </ItemGroup>
                </Project>
                """);
            var capability = MockCapability(hasCapability: true);

            var metadata = await CreateService(capability.Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsFalse(metadata.HasQtBridgeCapability);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.linux-x64", metadata.MatchedPackageId);
            capability.Verify(detector => detector.EvaluateAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Does_Not_Detect_Project_Without_Capability()
        {
            using var tempDir = new TempDir();
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj", PlainProject);

            var metadata = await CreateService(MockCapability(hasCapability: false).Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsFalse(metadata.IsQtBridgeProject);
            Assert.IsFalse(metadata.HasQtBridgeCapability);
            Assert.IsFalse(metadata.HasEvaluatedPackageReference);
        }

        [TestMethod]
        public async Task Detects_Project_By_Evaluated_PackageReference_Before_Restore()
        {
            // Before a restore, or when it fails, there is no capability, but the package
            // reference is already known.
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Directory.Build.props", QtBridgePackageReference);
            var projectPath = await WriteFileAsync(tempDir, Path.Combine("App", "App.csproj"),
                PlainProject);
            var detector = MockCapability(hasCapability: false,
                packageId: "QtGroup.Qt.Bridge.CSharp.linux-x64");

            var metadata = await CreateService(detector.Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.AreEqual(QtBridgeProjectType.QtBridgeCSharp, metadata.ProjectType);
            Assert.IsFalse(metadata.HasQtBridgeCapability);
            Assert.IsTrue(metadata.HasEvaluatedPackageReference);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.linux-x64", metadata.MatchedPackageId);
        }

        [TestMethod]
        public async Task Reports_Capability_And_Evaluated_PackageReference_Together()
        {
            using var tempDir = new TempDir();
            await WriteFileAsync(tempDir, "Directory.Build.props", QtBridgePackageReference);
            var projectPath = await WriteFileAsync(tempDir, Path.Combine("App", "App.csproj"),
                PlainProject);
            var detector = MockCapability(hasCapability: true,
                packageId: "QtGroup.Qt.Bridge.CSharp.linux-x64");

            var metadata = await CreateService(detector.Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsTrue(metadata.HasQtBridgeCapability);
            Assert.IsTrue(metadata.HasEvaluatedPackageReference);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.linux-x64", metadata.MatchedPackageId);
        }

        [TestMethod]
        public async Task Resolves_Templated_Package_Id_From_Evaluated_Project()
        {
            // Generated app templates reference $(QtBridgePackageId); only the evaluated
            // project knows the package ID it resolves to.
            using var tempDir = new TempDir();
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="$(QtBridgePackageId)" Version="0.5.0" />
                  </ItemGroup>
                  <PropertyGroup>
                    <QtBridgePackagePrefix>QtGroup.Qt.Bridge.CSharp</QtBridgePackagePrefix>
                  </PropertyGroup>
                </Project>
                """);
            var detector = MockCapability(hasCapability: true,
                packageId: "QtGroup.Qt.Bridge.CSharp.win-arm64");

            var metadata = await CreateService(detector.Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsTrue(metadata.HasQtBridgeCapability);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.win-arm64", metadata.MatchedPackageId);
        }

        [TestMethod]
        public async Task Keeps_Templated_Package_Id_When_Project_Is_Not_Loaded()
        {
            using var tempDir = new TempDir();
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="$(QtBridgePackageId)" Version="0.5.0" />
                  </ItemGroup>
                  <PropertyGroup>
                    <QtBridgePackagePrefix>QtGroup.Qt.Bridge.CSharp</QtBridgePackagePrefix>
                  </PropertyGroup>
                </Project>
                """);

            var metadata = await CreateService(MockCapability(hasCapability: false).Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.AreEqual("$(QtBridgePackageId)", metadata.MatchedPackageId);
        }

        [TestMethod]
        public async Task Skips_Capability_Query_When_Static_Analysis_Detects_Project()
        {
            using var tempDir = new TempDir();
            var projectPath = await WriteFileAsync(tempDir, "Sample.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="QtGroup.Qt.Bridge.CSharp.win-x64" Version="0.5.0" />
                  </ItemGroup>
                </Project>
                """);
            var capability = MockCapability(hasCapability: true);

            var metadata = await CreateService(capability.Object)
                .TryGetMetadataForPathAsync(projectPath, CancellationToken);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.win-x64", metadata.MatchedPackageId);
            capability.Verify(detector => detector.EvaluateAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
