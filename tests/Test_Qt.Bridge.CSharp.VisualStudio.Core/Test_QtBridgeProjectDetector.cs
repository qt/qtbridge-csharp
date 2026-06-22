// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    [TestClass]
    public class Test_QtBridgeProjectDetector
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Detects_Project_By_QtBridge_PackageReference()
        {
            using var tempDir = new TempDir();
            var projectPath = Path.Combine(tempDir.Path, "Sample.csproj");

            var cancellationToken = TestContext.CancellationTokenSource.Token;
            const string contents =
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="QtGroup.Qt.Bridge.CSharp.win-x64" Version="0.2.0.9-beta" />
              </ItemGroup>
            </Project>
            """;
            await File.WriteAllTextAsync(projectPath, contents, cancellationToken);

            var detector = new QtBridgeProjectDetector();
            var metadata = await detector.DetectAsync(projectPath, cancellationToken);

            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.AreEqual(QtBridgeProjectType.QtBridgeCSharp, metadata.ProjectType);
            Assert.AreEqual("QtGroup.Qt.Bridge.CSharp.win-x64", metadata.MatchedPackageId);
        }

        [TestMethod]
        public async Task Detects_Project_By_QtBridge_Property()
        {
            using var tempDir = new TempDir();
            var projectPath = Path.Combine(tempDir.Path, "Sample.csproj");

            var cancellationToken = TestContext.CancellationTokenSource.Token;
            await File.WriteAllTextAsync(projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <QtDotNetPropsImported>true</QtDotNetPropsImported>
                    <QtQmlRootModule>Application</QtQmlRootModule>
                  </PropertyGroup>
                </Project>
                """, cancellationToken);

            var detector = new QtBridgeProjectDetector();
            var metadata = await detector.DetectAsync(projectPath, cancellationToken);

            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsTrue(metadata.Properties.ContainsKey("QtDotNetPropsImported"));
            Assert.AreEqual("Application", metadata.Properties["QtQmlRootModule"]);
        }

        [TestMethod]
        public async Task Detects_Project_By_QtBridge_Import()
        {
            using var tempDir = new TempDir();
            var projectPath = Path.Combine(tempDir.Path, "Sample.csproj");

            var cancellationToken = TestContext.CancellationTokenSource.Token;
            await File.WriteAllTextAsync(projectPath,
                """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <Import Project="..\build\QtGroup.Qt.Bridge.CSharp.props" />
            </Project>
            """, cancellationToken);

            var detector = new QtBridgeProjectDetector();
            var metadata = await detector.DetectAsync(projectPath, cancellationToken);

            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsTrue(metadata.ImportsQtBridgeProps);
            Assert.IsFalse(metadata.ImportsQtBridgeTargets);
        }

        [TestMethod]
        public async Task Detects_Project_By_QtBridge_Targets_Import()
        {
            using var tempDir = new TempDir();
            var projectPath = Path.Combine(tempDir.Path, "Sample.csproj");

            var cancellationToken = TestContext.CancellationTokenSource.Token;
            await File.WriteAllTextAsync(projectPath,
                """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <Import Project="..\build\Qt.Bridge.targets" />
            </Project>
            """, cancellationToken);

            var detector = new QtBridgeProjectDetector();
            var metadata = await detector.DetectAsync(projectPath, cancellationToken);

            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.IsFalse(metadata.ImportsQtBridgeProps);
            Assert.IsTrue(metadata.ImportsQtBridgeTargets);
        }

        [TestMethod]
        public async Task Detects_Project_By_Templated_QtBridge_PackageReference()
        {
            using var tempDir = new TempDir();
            var projectPath = Path.Combine(tempDir.Path, "Sample.csproj");

            var cancellationToken = TestContext.CancellationTokenSource.Token;
            await File.WriteAllTextAsync(projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <QtBridgePackagePrefix>QtGroup.Qt.Bridge.CSharp.</QtBridgePackagePrefix>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="$(QtBridgePackageId)" Version="1.0.0" />
                  </ItemGroup>
                </Project>
                """, cancellationToken);

            var detector = new QtBridgeProjectDetector();
            var metadata = await detector.DetectAsync(projectPath, cancellationToken);

            Assert.IsTrue(metadata.IsQtBridgeProject);
            Assert.AreEqual(QtBridgeProjectType.QtBridgeCSharp, metadata.ProjectType);
            Assert.AreEqual("$(QtBridgePackageId)", metadata.MatchedPackageId);
        }

        [TestMethod]
        public async Task Does_Not_Detect_Plain_DotNet_Project()
        {
            using var tempDir = new TempDir();
            var projectPath = Path.Combine(tempDir.Path, "Sample.csproj");

            var cancellationToken = TestContext.CancellationTokenSource.Token;
            await File.WriteAllTextAsync(projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                  </ItemGroup>
                </Project>
                """, cancellationToken);

            var detector = new QtBridgeProjectDetector();
            var metadata = await detector.DetectAsync(projectPath, cancellationToken);

            Assert.IsFalse(metadata.IsQtBridgeProject);
            Assert.AreEqual(QtBridgeProjectType.Unknown, metadata.ProjectType);
            Assert.IsNull(metadata.MatchedPackageId);
        }
    }
}
