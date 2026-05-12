// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    /// <summary>
    /// Strongly-typed model of the <c>qtbridge-qml.ide.json</c> metadata file produced by the
    /// Qt Bridge MSBuild target. Drives QML Language Server startup and workspace configuration.
    /// </summary>
    public sealed class QmlMetadata(
        int version,
        string projectFile,
        string configuration,
        string? targetFramework,
        QmlMetadata.QmlSection qml,
        QmlMetadata.QmlLanguageServerSection qmlLanguageServer)
    {
        public int Version { get; } = version;
        public string ProjectFile { get; } = projectFile
            ?? throw new ArgumentNullException(nameof(projectFile));
        public string Configuration { get; } = configuration
            ?? throw new ArgumentNullException(nameof(configuration));
        public string? TargetFramework { get; } = targetFramework;
        public QmlSection Qml { get; } = qml ?? throw new ArgumentNullException(nameof(qml));
        public QmlLanguageServerSection QmlLanguageServer { get; } = qmlLanguageServer
            ?? throw new ArgumentNullException(nameof(qmlLanguageServer));

        /// <summary>QML source and build directory information from the metadata file.</summary>
        public sealed class QmlSection(
            string sourceDir,
            string? projectSourceDir,
            IReadOnlyList<string> buildDirs,
            IReadOnlyList<string> importPaths,
            IReadOnlyList<QmlFile> files)
        {
            /// <summary>
            /// The generated Qt-native source root used as the primary QML Language Server
            /// workspace. Matches the section key in .qt/.qmlls.build.ini.
            /// Example: obj\x64\Debug\net8.0\qt\native\source
            /// </summary>
            public string SourceDir { get; } = sourceDir
                ?? throw new ArgumentNullException(nameof(sourceDir));

            /// <summary>
            /// The original user project source root. Used for runtime $/addBuildDirs mapping so
            /// the QML Language Server covers user-authored files.
            /// Example: C:\MyProject
            /// </summary>
            public string? ProjectSourceDir { get; } = projectSourceDir;

            /// <summary>
            /// One or more Qt-native build directories containing .qt/.qmlls.build.ini.
            /// Example: obj\x64\Debug\net8.0\qt\native\build
            /// </summary>
            public IReadOnlyList<string> BuildDirs { get; } = buildDirs
                ?? throw new ArgumentNullException(nameof(buildDirs));

            /// <summary>
            /// Additional QML import paths to pass to the language server via <c>-I</c>.
            /// Suppresses the CI-baked fallback path compiled into the qmlls binary.
            /// Sourced from the same MSBuild properties that write .qt/.qmlls.build.ini.
            /// </summary>
            public IReadOnlyList<string> ImportPaths { get; } = importPaths
                ?? throw new ArgumentNullException(nameof(importPaths));

            /// <summary>
            /// Original project QML files and their generated module locations. Used by the
            /// Visual Studio extension to map editor files back into the generated QML module
            /// resource tree for qmlls.
            /// </summary>
            public IReadOnlyList<QmlFile> Files { get; } = files
                ?? throw new ArgumentNullException(nameof(files));
        }

        public sealed class QmlFile(
            string sourcePath,
            string uri,
            string typeName,
            string modulePath)
        {
            public string SourcePath { get; } = sourcePath
                ?? throw new ArgumentNullException(nameof(sourcePath));
            public string Uri { get; } = uri
                ?? throw new ArgumentNullException(nameof(uri));
            public string TypeName { get; } = typeName
                ?? throw new ArgumentNullException(nameof(typeName));
            public string ModulePath { get; } = modulePath
                ?? throw new ArgumentNullException(nameof(modulePath));
        }

        /// <summary> QML Language Server startup policy from the metadata file. </summary>
        public sealed class QmlLanguageServerSection(bool disableCMakeCalls)
        {
            /// <summary>
            /// Whether to launch the QML Language Server executable with --no-cmake-calls.
            /// Defaults to true for qtbridge-csharp projects.
            /// </summary>
            public bool DisableCMakeCalls { get; } = disableCMakeCalls;
        }
    }
}
