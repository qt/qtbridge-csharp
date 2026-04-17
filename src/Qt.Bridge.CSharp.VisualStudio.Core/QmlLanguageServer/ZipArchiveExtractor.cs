// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.IO.Compression;

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Extracts zip archives asynchronously ensuring each entry's resolved path is verified to
    /// remain inside the extraction root before writing.
    /// </summary>
    public sealed class ZipArchiveExtractor : IZipArchiveExtractor
    {
        private const int BufferSize = 81920;

        public async Task ExtractAsync(
            string archivePath,
            string targetDir,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(archivePath))
                throw new ArgumentException("An archive path is required.", nameof(archivePath));

            if (string.IsNullOrWhiteSpace(targetDir))
                throw new ArgumentException("A target directory is required.", nameof(targetDir));

            var extractionRoot = Directory.CreateDirectory(targetDir).FullName;
            var normalizedRoot = EnsureTrailingDirectorySeparator(
                Path.GetFullPath(extractionRoot));

            using var sourceStream = new FileStream(
                archivePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                useAsync: true);

            using var archive = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: false);
            foreach (var entry in archive.Entries) {
                cancellationToken.ThrowIfCancellationRequested();
                await ExtractEntryAsync(entry, normalizedRoot, cancellationToken);
            }
        }

        private static async Task ExtractEntryAsync(
            ZipArchiveEntry entry,
            string extractionRoot,
            CancellationToken cancellationToken)
        {
            var fullName = entry.FullName;
            var destinationPath = Path.GetFullPath(Path.Combine(extractionRoot, fullName));
            if (!destinationPath.StartsWith(extractionRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Archive entry escapes extraction root: {fullName}");

            if (string.IsNullOrEmpty(entry.Name)) {
                Directory.CreateDirectory(destinationPath);
                return;
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
                throw new InvalidDataException($"Archive entry has no destination dir: {fullName}");

            Directory.CreateDirectory(destinationDirectory);

            using (var entryStream = entry.Open())
            using (var destinationStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true)) {
                await entryStream.CopyToAsync(destinationStream, BufferSize, cancellationToken);
            }

            File.SetLastWriteTimeUtc(destinationPath, entry.LastWriteTime.UtcDateTime);
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
