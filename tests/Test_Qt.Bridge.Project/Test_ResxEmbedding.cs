// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public class Test_ResxEmbedding
    {
        private const string SysString = "System.String, mscorlib";
        private const string Version = "Version=4.0.0.0";
        private const string Culture = "Culture=neutral";
        private const string PublicKeyToken = "PublicKeyToken=b77a5c561934e089";

        // Minimal ResX with a single ResXFileRef entry.
        private static string ResxWithFile(string fileName, string key = "Entry") =>
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <root>
               <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
               <resheader name="version"><value>2.0</value></resheader>
               <data name="{key}" type="System.Resources.ResXFileRef, System.Windows.Forms">
                 <value>{fileName};{SysString},{Version},{Culture} {PublicKeyToken};utf-8</value>
               </data>
             </root>
             """;

        // Three separate .resx files - one per access mode - so per-file embedding assertions
        // are unambiguous. With all three modes in one file the whole file gets embedded
        // the moment any ManagedAndNative or ManagedOnly entry is present, which would make
        // the Default assertion meaningless.
        private static string ProjectXml =>
            """
              <PropertyGroup>
                <QtBridgeResourceLibrary>true</QtBridgeResourceLibrary>
              </PropertyGroup>
              <ItemGroup>
                <QtResourceAccess Include="ResDefault.resx::Entry" Mode="Default" />
                <QtResourceAccess Include="ResManagedOnly.resx::Entry" Mode="ManagedOnly" />
                <QtResourceAccess Include="ResManagedAndNative.resx::Entry" Mode="ManagedAndNative" />
              </ItemGroup>
              <Target Name="InspectEmbeddingCandidates"
                      DependsOnTargets="QtBridgeAddResources">
                <Message Text="EmbeddedResxFile: %(_ManagedEmbeddedResxFile.Filename)%(Extension)"
                         Importance="High" />
              </Target>
              <Target Name="InspectFinalEmbeddedResources"
                      DependsOnTargets="QtBridgeAddResources">
                <Message Text="FinalEmbedded: %(EmbeddedResource.Filename)%(EmbeddedResource.Extension)"
                         Importance="High" />
              </Target>
            """;

        private static void AddResxFiles(TempProject temp)
        {
            temp.AddFile("ResDefault.resx", ResxWithFile("default.txt"));
            temp.AddFile("default.txt", "default content");
            temp.AddFile("ResManagedOnly.resx", ResxWithFile("managed-only.txt"));
            temp.AddFile("managed-only.txt", "managed-only content");
            temp.AddFile("ResManagedAndNative.resx", ResxWithFile("managed-and-native.txt"));
            temp.AddFile("managed-and-native.txt", "managed-and-native content");
        }

        private static TempProject CreateProject()
        {
            var temp = new TempProject();
            temp.Create(new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                AfterSdkTargets = ProjectXml
            });
            AddResxFiles(temp);
            return temp;
        }

        private static BuildOptions BuildTarget(string target) => new()
        {
            Targets = [target],
            TargetPath = "",
            TargetExePath = ""
        };

        private static bool HasLoggedItem(string output, string prefix, string fileName) =>
            Regex.IsMatch(output, $@"\b{Regex.Escape(prefix)}:\s*{Regex.Escape(fileName)}\b");

        /// <summary>
        /// Target-level test: verifies that <c>QtBridgeResolveResxFileRefs</c> populates
        /// <c>ManagedEmbeddedResxFiles</c> according to policy - <c>ManagedOnly</c> and
        /// <c>ManagedAndNative</c> entries trigger managed embedding; <c>Default</c> does not.
        ///
        /// This is a narrow test of the MSBuild target's item computation. It does not run the
        /// compiler and is therefore fast and independent of the Qt native toolchain.
        /// </summary>
        [TestMethod]
        public async Task EmbeddingCandidates_MatchPolicy()
        {
            using var temp = CreateProject();

            var (ok, output) = await temp.BuildAsync(BuildTarget("InspectEmbeddingCandidates"));
            temp.SaveLog();
            Assert.IsTrue(ok, output);

            Assert.IsTrue(temp.Log.TryFindTarget("InspectEmbeddingCandidates", out var logTarget),
                "InspectEmbeddingCandidates target did not run");

            Assert.IsTrue(logTarget.HasMessage(new Regex(@"\bResManagedOnly\.resx\b")),
                "ResManagedOnly.resx must be an embedding candidate");
            Assert.IsTrue(logTarget.HasMessage(new Regex(@"\bResManagedAndNative\.resx\b")),
                "ResManagedAndNative.resx must be an embedding candidate");
            Assert.IsFalse(logTarget.HasMessage(new Regex(@"\bResDefault\.resx\b")),
                "ResDefault.resx must not be an embedding candidate");
        }

        /// <summary>
        /// Item-mutation test: verifies that the dynamic <c>EmbeddedResource</c> modifications
        /// inside <c>QtBridgeAddResources</c> take effect - the policy-driven remove/re-include
        /// leaves <c>ManagedOnly</c> and <c>ManagedAndNative</c> files in <c>EmbeddedResource</c>
        /// and drops the <c>Default</c> file.
        ///
        /// <c>GenerateResource</c> runs before <c>QtBridgeAddResources</c> in the SDK pipeline
        /// (it is a dependency of <c>CoreCompile</c>, while <c>QtBridgeAddResources</c> only has
        /// <c>BeforeTargets="CoreCompile"</c>). Checking <c>EmbeddedResource</c> items after the
        /// target is therefore the correct observable: it proves the mutations ran and will be
        /// seen by subsequent <c>CoreCompile</c> invocations, not by <c>GenerateResource</c>.
        /// </summary>
        [TestMethod]
        public async Task EmbeddedResource_UpdatedByAddResources()
        {
            using var temp = CreateProject();

            var (ok, output) = await temp.BuildAsync(BuildTarget("InspectFinalEmbeddedResources"));
            temp.SaveLog();
            Assert.IsTrue(ok, output);

            Assert.IsTrue(HasLoggedItem(output, "FinalEmbedded", "ResManagedOnly.resx"),
                "ResManagedOnly.resx must remain in EmbeddedResource after QtBridgeAddResources");
            Assert.IsTrue(HasLoggedItem(output, "FinalEmbedded", "ResManagedAndNative.resx"),
                "ResManagedAndNative.resx must remain in EmbeddedResource after QtBridgeAddResources");
            Assert.IsFalse(HasLoggedItem(output, "FinalEmbedded", "ResDefault.resx"),
                "ResDefault.resx must be removed from EmbeddedResource by QtBridgeAddResources");
        }

        [TestMethod]
        public async Task ResxFileRef_WithWindowsSeparators_ResolvesOnLinux()
        {
            using var temp = new TempProject();
            temp.Create(new CreationOptions
            {
                PackageReferences = [Packages.QtBridge],
                AfterSdkTargets =
                    """
                      <PropertyGroup>
                        <QtBridgeResourceLibrary>true</QtBridgeResourceLibrary>
                      </PropertyGroup>
                      <ItemGroup>
                        <QtResourceAccess Include="Books.resx::SynopsisHistory" Mode="ManagedAndNative" />
                      </ItemGroup>
                      <Target Name="InspectResolvedResources"
                              DependsOnTargets="QtBridgeAddResources">
                        <Message Text="ResolvedResource: %(_QtResolvedResxResource.Key)"
                                 Importance="High" />
                      </Target>
                    """
            });

            temp.AddFile("Books.resx", ResxWithFile(@"synopsis\history.txt", "SynopsisHistory"));
            temp.AddFile("synopsis/history.txt", "history content");

            var (ok, output) = await temp.BuildAsync(BuildTarget("InspectResolvedResources"));
            temp.SaveLog();
            Assert.IsTrue(ok, output);
            Assert.IsTrue(HasLoggedItem(output, "ResolvedResource", "Books.resx::SynopsisHistory"),
                "Windows-style ResXFileRef path separators must resolve on non-Windows hosts");
        }
    }
}
