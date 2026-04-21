// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    /// <summary>
    /// Reads and validates the build-generated <c>qtbridge-qml.ide.json</c> metadata file that
    /// configures the QML Language Server for a Qt Bridge project.
    /// </summary>
    public interface IQmlMetadataReader
    {
        /// <summary>
        /// Searches for the metadata file under the project's <c>obj\</c> directory whose
        /// containing path ends with <paramref name="configKey"/>. Handles both
        /// <c>obj\Debug\</c> and <c>obj\x64\Debug\</c> layouts without requiring knowledge of
        /// <c>BaseIntermediateOutputPath</c>.
        /// <para>
        /// <paramref name="configKey"/> may be a bare configuration name (e.g. <c>Debug</c>)
        /// or a platform-qualified key (e.g. <c>x64\Debug</c>). When only the configuration
        /// is given and multiple platform directories match (e.g. <c>obj\x64\Debug\</c> and
        /// <c>obj\arm64\Debug\</c>), returns <c>null</c> rather than silently picking the
        /// wrong build.
        /// </para>
        /// </summary>
        string? FindMetadataFilePath(string projectDirectory, string configKey);

        /// <summary>
        /// Reads and deserializes the metadata file at the given path. Returns a result with
        /// <see cref="QmlMetadataReadError.NotFound"/> if the file does not exist,
        /// <see cref="QmlMetadataReadError.IoError"/> on read error, or
        /// <see cref="QmlMetadataReadError.ParseError"/> if deserialization fails.
        /// Does not perform semantic validation - call <see cref="Validate"/> separately.
        /// </summary>
        QmlMetadataReadResult TryRead(string metadataFilePath, CancellationToken ct = default);
        /// <summary>
        /// Validates a deserialized metadata file against the active project context.
        /// Checks: version == 1, projectFile and configuration match, sourceDir and all buildDirs
        /// exist on disk. Returns false for any missing or mismatched field - caller should treat
        /// as missing/stale.
        /// </summary>
        bool Validate(QmlMetadata metadata, string projectFilePath, string configuration);
    }
}
