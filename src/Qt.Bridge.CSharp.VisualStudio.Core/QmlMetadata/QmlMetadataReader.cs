// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata
{
    /// <summary>
    /// Reads and validates <c>qtbridge-qml.ide.json</c> metadata files using
    /// <see cref="System.Runtime.Serialization.Json.DataContractJsonSerializer"/>.
    /// </summary>
    public sealed class QmlMetadataReader : IQmlMetadataReader
    {
        public const string MetadataFileName = "qtbridge-qml.ide.json";

        /// <summary>
        /// Searches <c>obj\</c> under <paramref name="projectDir"/> for a metadata file
        /// whose containing directory path ends with <paramref name="configKey"/>.
        /// <para>
        /// <paramref name="configKey"/> may be a bare configuration name (e.g. <c>Debug</c>)
        /// or a platform-qualified key (e.g. <c>x64\Debug</c>). When a platform is supplied
        /// the match is unambiguous by definition; when only the configuration is given, at
        /// most one match is accepted - two matches (different platform directories) return
        /// <c>null</c> rather than silently picking the wrong one.
        /// </para>
        /// </summary>
        public string? FindMetadataFilePath(string projectDir, string configKey)
        {
            if (string.IsNullOrWhiteSpace(projectDir) || string.IsNullOrWhiteSpace(configKey))
                return null;

            var objDir = Path.Combine(projectDir, "obj");
            if (!Directory.Exists(objDir))
                return null;

            // Normalize separators so comparison works on both Windows and non-Windows.
            var normalizedKey = configKey.Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            // Prefer the exact canonical path obj\<configKey>\MetadataFileName>. This ensures Any
            // CPU builds (which write obj\Debug\) are found even when a stale platform-qualified
            // file (obj\x64\Debug\) is also present.
            var exactPath = Path.Combine(objDir, normalizedKey, MetadataFileName);
            if (File.Exists(exactPath))
                return exactPath;

            // Fall back to a recursive tail-match for cases where BaseIntermediateOutputPath
            // includes additional segments not captured by configKey alone. Take at most 2 to
            // detect ambiguity without enumerating everything.
            var matches = Directory
                .EnumerateFiles(objDir, MetadataFileName, SearchOption.AllDirectories)
                .Where(f => {
                    var dir = Path.GetDirectoryName(f);
                    if (dir == null)
                        return false;
                    return dir.EndsWith(Path.DirectorySeparatorChar + normalizedKey,
                        StringComparison.OrdinalIgnoreCase);
                })
                .Take(2)
                .ToList();

            return matches.Count == 1 ? matches[0] : null;
        }

        public QmlMetadataReadResult TryRead(string? metadataFilePath, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var path = metadataFilePath ?? "";
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return QmlMetadataReadResult.Fail(QmlMetadataReadError.NotFound, path);

            try {
                using var stream = File.OpenRead(path);
                var serializer = new DataContractJsonSerializer(typeof(MetadataDto));
                var metadata = serializer.ReadObject(stream) is MetadataDto dto
                    ? FromDto(dto)
                    : null;
                return metadata != null
                    ? QmlMetadataReadResult.Ok(metadata, path)
                    : QmlMetadataReadResult.Fail(QmlMetadataReadError.ParseError, path);
            } catch (IOException ex) {
                return QmlMetadataReadResult.Fail(QmlMetadataReadError.IoError, path, ex);
            } catch (SerializationException ex) {
                return QmlMetadataReadResult.Fail(QmlMetadataReadError.ParseError, path, ex);
            }
        }

        public bool Validate(QmlMetadata metadata, string projectFilePath, string config)
        {
            if (string.IsNullOrWhiteSpace(projectFilePath) || string.IsNullOrWhiteSpace(config))
                return false;

            if (metadata.Version != 1)
                return false;

            if (!string.Equals(Path.GetFullPath(metadata.ProjectFile),
                Path.GetFullPath(projectFilePath),
                StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            if (!string.Equals(metadata.Configuration, config, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!Directory.Exists(metadata.Qml.SourceDir))
                return false;

            return metadata.Qml.BuildDirs.Count != 0 && metadata.Qml.BuildDirs.All(Directory.Exists);
        }

        private static QmlMetadata? FromDto(MetadataDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProjectFile)
                || string.IsNullOrWhiteSpace(dto.Configuration)
                || string.IsNullOrWhiteSpace(dto.Qml?.SourceDir)
                || dto.Qml?.BuildDirs is not { Length: > 0 }) {
                return null;
            }

            var buildDirs = dto.Qml!.BuildDirs!
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToArray();

            if (buildDirs.Length == 0)
                return null;

            return new QmlMetadata(
                version: dto.Version,
                projectFile: dto.ProjectFile!,
                configuration: dto.Configuration!,
                targetFramework: dto.TargetFramework,
                qml: new QmlMetadata.QmlSection(
                    sourceDir: dto.Qml!.SourceDir!,
                    projectSourceDir: dto.Qml.ProjectSourceDir,
                    buildDirs: buildDirs,
                    importPaths: dto.Qml.ImportPaths?
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToArray() ?? [],
                    files: dto.Qml.Files?
                        .Where(f => !string.IsNullOrWhiteSpace(f.SourcePath)
                            && !string.IsNullOrWhiteSpace(f.Uri)
                            && !string.IsNullOrWhiteSpace(f.TypeName)
                            && !string.IsNullOrWhiteSpace(f.ModulePath))
                        .Select(f => new QmlMetadata.QmlFile(
                            f.SourcePath!,
                            f.Uri!,
                            f.TypeName!,
                            f.ModulePath!))
                        .ToArray() ?? []),
                qmlLanguageServer: new QmlMetadata.QmlLanguageServerSection(
                    disableCMakeCalls: dto.QmlLanguageServer?.DisableCMakeCalls
                        ?? dto.LegacyQmlLanguageServer?.DisableCMakeCalls ?? true));
        }

        [DataContract]
        private sealed class MetadataDto
        {
            [DataMember(Name = "version")]
            public int Version { get; set; }

            [DataMember(Name = "projectFile")]
            public string? ProjectFile { get; set; }

            [DataMember(Name = "configuration")]
            public string? Configuration { get; set; }

            [DataMember(Name = "targetFramework")]
            public string? TargetFramework { get; set; }

            [DataMember(Name = "qml")]
            public QmlDto? Qml { get; set; }

            [DataMember(Name = "qmlLanguageServer")]
            public QmlLanguageServerDto? QmlLanguageServer { get; set; }

            [DataMember(Name = "qmlls")]
            public QmlLanguageServerDto? LegacyQmlLanguageServer { get; set; }
        }

        [DataContract]
        private sealed class QmlDto
        {
            [DataMember(Name = "sourceDir")]
            public string? SourceDir { get; set; }

            [DataMember(Name = "projectSourceDir")]
            public string? ProjectSourceDir { get; set; }

            [DataMember(Name = "importPaths")]
            public string[]? ImportPaths { get; set; }

            [DataMember(Name = "buildDirs")]
            public string[]? BuildDirs { get; set; }

            [DataMember(Name = "files")]
            public QmlFileDto[]? Files { get; set; }
        }

        [DataContract]
        private sealed class QmlFileDto
        {
            [DataMember(Name = "sourcePath")]
            public string? SourcePath { get; set; }

            [DataMember(Name = "uri")]
            public string? Uri { get; set; }

            [DataMember(Name = "typeName")]
            public string? TypeName { get; set; }

            [DataMember(Name = "modulePath")]
            public string? ModulePath { get; set; }
        }

        [DataContract]
        private sealed class QmlLanguageServerDto
        {
            [DataMember(Name = "disableCMakeCalls")]
            public bool? DisableCMakeCalls { get; set; }
        }
    }
}
