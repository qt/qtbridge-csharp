// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Downloads, verifies (SHA-256), extracts, and installs the QML Language Server. Uses a
    /// staging directory to avoid leaving a corrupt installation as the active version, and
    /// persists an installation manifest for subsequent fast-path reuse.
    /// </summary>
    /// <remarks>
    /// If the latest release cannot be fetched or installed, for example offline, the last
    /// installed version is used, however old it is.
    /// </remarks>
    public sealed class QmlLanguageServerInstaller(
        IReleaseMetadataClient releaseClient,
        IZipArchiveExtractor archiveExtractor,
        string rootDirectory)
        : IQmlLanguageServerInstaller
    {
        public QmlLanguageServerInstaller(
            IReleaseMetadataClient releaseClient,
            IZipArchiveExtractor archiveExtractor)
            : this(releaseClient, archiveExtractor, QmlLanguageServerPaths.RootDirectory)
        {}

        private static readonly HttpClient HttpClient = new() {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly SemaphoreSlim installationLock = new(1, 1);
        private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24);
        private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(10);

        private readonly IReleaseMetadataClient releaseClient = releaseClient
            ?? throw new ArgumentNullException(nameof(releaseClient));

        private readonly IZipArchiveExtractor archiveExtractor = archiveExtractor
            ?? throw new ArgumentNullException(nameof(archiveExtractor));

        private readonly string rootDirectory = rootDirectory
            ?? throw new ArgumentNullException(nameof(rootDirectory));

        public async Task<QmlLanguageServerInstallation> EnsureInstalledAsync(CancellationToken ct)
        {
            await installationLock.WaitAsync(ct);
            try {
                if (TryGetCurrentInstallation(CacheMaxAge, out var cached))
                    return cached!;

                QmlLanguageServerInstallation install;
                try {
                    install = await GetLatestInstallationAsync(ct);
                } catch (QmlLanguageServerInstallException ex)
                    when (TryGetCurrentInstallation(maxAge: null, out var previous)) {
                    // The manifest keeps its check time, so the next start tries again.
                    previous!.UpdateError = ex;
                    return previous;
                }
                try {
                    await WriteCurrentManifestGuardedAsync(install, ct);
                } catch (QmlLanguageServerInstallException) {
                    // The installation works without the cached check; the next call checks
                    // again.
                }
                return install;
            } finally {
                installationLock.Release();
            }
        }

        private async Task<QmlLanguageServerInstallation> GetLatestInstallationAsync(
            CancellationToken ct)
        {
            QmlLanguageServerRelease latestRelease;
            try {
                latestRelease = await releaseClient.GetLatestReleaseAsync(ct);
            } catch (QmlLanguageServerAssetException ex) {
                throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.NoMatchingAsset,
                    "No QML Language Server package found for this platform.", ex);
            } catch (Exception ex) when (ex is not QmlLanguageServerInstallException
                && !IsCancelledBy(ct, ex)) {
                throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.ReleaseMetadataUnavailable,
                    "Could not fetch QML Language Server release metadata.", ex);
            }

            var installDir = QmlLanguageServerPaths.GetInstallDirectory(rootDirectory,
                latestRelease.TagName);
            if (TryReadMatchingInstallation(installDir, latestRelease, out var existing))
                return existing;
            return await InstallReleaseAsync(latestRelease, installDir, ct);
        }

        private async Task WriteCurrentManifestGuardedAsync(
            QmlLanguageServerInstallation installation,
            CancellationToken ct)
        {
            try {
                await WriteCurrentInstallationManifestAsync(installation, ct);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.ManifestWriteFailed,
                    "Could not save the QML Language Server install manifest.", ex) {
                    InstallDirectory = installation.InstallDirectory
                };
            }
        }

        private async Task<QmlLanguageServerInstallation> InstallReleaseAsync(
            QmlLanguageServerRelease release,
            string installDirectory,
            CancellationToken ct)
        {
            try {
                Directory.CreateDirectory(rootDirectory);
                Directory.CreateDirectory(QmlLanguageServerPaths.GetVersionsDirectory(rootDirectory));
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.InstallDirectoryAccessDenied,
                    "Cannot create the QML Language Server install directory.", ex) {
                    InstallDirectory = rootDirectory
                };
            }

            var stagingDirectory = installDirectory + ".tmp-" + Guid.NewGuid().ToString("N");
            var downloadPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");

            try {
                try {
                    await DownloadArchiveAsync(release.Asset.DownloadUrl, downloadPath, ct);
                } catch (Exception ex) when (ex is not QmlLanguageServerInstallException
                    && !IsCancelledBy(ct, ex)) {
                    throw new QmlLanguageServerInstallException(
                        QmlLanguageServerInstallError.DownloadFailed, "Could not download the QML "
                            + $"Language Server from '{release.Asset.DownloadUrl}'.", ex) {
                        AssetName = release.Asset.Name,
                        DownloadUrl = release.Asset.DownloadUrl
                    };
                }

                // VerifyArchiveDigestAsync already throws QmlLanguageServerInstallException
                // with DigestMismatch for verification failures.
                await VerifyArchiveDigestAsync(downloadPath, release.Asset.Sha256Digest, ct);

                try {
                    if (Directory.Exists(stagingDirectory))
                        Directory.Delete(stagingDirectory, recursive: true);
                    Directory.CreateDirectory(stagingDirectory);
                    await archiveExtractor.ExtractAsync(downloadPath, stagingDirectory, ct);
                } catch (Exception ex) when (ex is not OperationCanceledException
                    and not QmlLanguageServerInstallException) {
                    throw new QmlLanguageServerInstallException(
                        QmlLanguageServerInstallError.ExtractionFailed,
                        "Could not extract the QML Language Server package.", ex) {
                        StagingDirectory = stagingDirectory,
                        AssetName = release.Asset.Name
                    };
                }

                // FindExecutablePath throws QmlLanguageServerInstallException(ExecutableNotFound).
                var executablePath = FindExecutablePath(stagingDirectory);

                try {
                    if (Directory.Exists(installDirectory))
                        Directory.Delete(installDirectory, recursive: true);
                    Directory.Move(stagingDirectory, installDirectory);
                } catch (Exception ex) when (ex is not OperationCanceledException) {
                    throw new QmlLanguageServerInstallException(
                        QmlLanguageServerInstallError.InstallDirectoryAccessDenied,
                        "Could not move staged QML Language Server to install directory.", ex) {
                        InstallDirectory = installDirectory,
                        StagingDirectory = stagingDirectory
                    };
                }

                // Rebase executable path from staging to final install directory.
                // Paths come from Directory.EnumerateFiles on the same machine so casing matches.
                executablePath = executablePath.Replace(stagingDirectory, installDirectory);

                var finalInstallation = new QmlLanguageServerInstallation(
                    release.TagName,
                    release.ReleaseId,
                    installDirectory,
                    executablePath,
                    release.Asset.Name,
                    release.Asset.DownloadUrl,
                    release.Asset.Sha256Digest,
                    DateTimeOffset.UtcNow);

                try {
                    await WriteInstallationManifestAsync(finalInstallation, installDirectory, ct);
                } catch (Exception ex) when (ex is not OperationCanceledException) {
                    throw new QmlLanguageServerInstallException(
                        QmlLanguageServerInstallError.ManifestWriteFailed,
                        "Could not save the QML Language Server install manifest.", ex) {
                        InstallDirectory = installDirectory
                    };
                }

                return finalInstallation;
            } finally {
                try {
                    if (File.Exists(downloadPath))
                        File.Delete(downloadPath);
                } catch (Exception) {}

                try {
                    if (Directory.Exists(stagingDirectory))
                        Directory.Delete(stagingDirectory, recursive: true);
                } catch (Exception) { }
            }
        }

        private static bool TryReadMatchingInstallation(
            string installDirectory,
            QmlLanguageServerRelease release,
            out QmlLanguageServerInstallation installation)
        {
            installation = null!;

            if (!Directory.Exists(installDirectory))
                return false;

            var manifestPath = QmlLanguageServerPaths.GetInstallationManifestPath(installDirectory);
            if (!File.Exists(manifestPath))
                return false;

            var manifest = ReadInstallationManifest(manifestPath);
            if (manifest == null)
                return false;

            if (!string.Equals(manifest.Version, release.TagName, StringComparison.Ordinal))
                return false;
            if (!string.Equals(manifest.ReleaseId, release.ReleaseId, StringComparison.Ordinal))
                return false;
            if (!string.Equals(manifest.AssetName, release.Asset.Name, StringComparison.Ordinal))
                return false;

            var sha = manifest.Sha256Digest;
            if (!string.Equals(sha, release.Asset.Sha256Digest, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(manifest.ExecutablePath))
                return false;
            if (!File.Exists(manifest.ExecutablePath))
                return false;

            installation = manifest;
            return true;
        }

        // HttpClient.Timeout ends at the response headers here; the body has its own limit.
        private static async Task DownloadArchiveAsync(
            string downloadUrl,
            string destinationPath,
            CancellationToken callerToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
            timeout.CancelAfter(DownloadTimeout);
            var cancellationToken = timeout.Token;
            using var response = await HttpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var destinationStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await responseStream.CopyToAsync(destinationStream, 81920, cancellationToken);
        }

        // HttpClient reports its timeouts as cancellations too; only the caller's token is a
        // cancellation that must not fall back to the installed version.
        private static bool IsCancelledBy(CancellationToken ct, Exception ex) =>
            ex is OperationCanceledException && ct.IsCancellationRequested;

        private static async Task VerifyArchiveDigestAsync(
            string archivePath,
            string expectedDigest,
            CancellationToken ct)
        {
            const int bufferSize = 81920;
            using var sha256 = SHA256.Create();
            using var archiveStream = new FileStream(
                archivePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: bufferSize,
                useAsync: true);

            var buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await archiveStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
                ct.ThrowIfCancellationRequested();
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock([], 0, 0);
            var hashBytes = sha256.Hash
                ?? throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.DigestMismatch,
                    "Failed to compute QML Language Server archive digest.");
            var actualDigest = ToHexString(hashBytes);
            if (!string.Equals(actualDigest, expectedDigest, StringComparison.OrdinalIgnoreCase)) {
                throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.DigestMismatch,
                    "QML Language Server archive digest mismatch."
                    + $" Expected '{expectedDigest}', got '{actualDigest}'.");
            }
        }

        private static string FindExecutablePath(string installDir)
        {
            var executablePaths = QmlLanguageServerPaths.GetCandidateExecutableNames()
                .SelectMany(name => Directory.EnumerateFiles(
                    installDir,
                    name,
                    SearchOption.AllDirectories))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            switch (executablePaths.Length) {
            case 1:
                return executablePaths[0];
            case 0: {
                    var candidates = string.Join(", ",
                        QmlLanguageServerPaths.GetCandidateExecutableNames());
                    throw new QmlLanguageServerInstallException(
                        QmlLanguageServerInstallError.ExecutableNotFound,
                        $"Could not locate any of [{candidates}]"
                        + " in the extracted QML Language Server package.") {
                        InstallDirectory = installDir
                    };
                }
            default:
                throw new QmlLanguageServerInstallException(
                    QmlLanguageServerInstallError.ExecutableNotFound,
                    "The extracted QML Language Server package contained"
                    + " multiple candidate executables: "
                    + string.Join(", ", executablePaths)) {
                    InstallDirectory = installDir
                };
            }
        }

        // Without maxAge, any installation whose executable still exists qualifies.
        private bool TryGetCurrentInstallation(
            TimeSpan? maxAge,
            out QmlLanguageServerInstallation? install)
        {
            install = null;
            var dto = ReadRawManifest(QmlLanguageServerPaths.GetCurrentManifestPath(rootDirectory));
            if (dto == null)
                return false;
            if (maxAge != null && (dto.LastCheckedAtUtc == null
                || DateTimeOffset.UtcNow - dto.LastCheckedAtUtc.Value >= maxAge)) {
                return false;
            }
            install = ToInstallation(dto);
            return install != null && File.Exists(install.ExecutablePath);
        }

        private static QmlLanguageServerInstallation? ReadInstallationManifest(string manifestPath)
            => ToInstallation(ReadRawManifest(manifestPath));

        private static InstallationManifestDto? ReadRawManifest(string manifestPath)
        {
            try {
                using var manifestStream = File.OpenRead(manifestPath);
                var serializer = new DataContractJsonSerializer(typeof(InstallationManifestDto));
                return serializer.ReadObject(manifestStream) as InstallationManifestDto;
            } catch (IOException) {
                return null;
            } catch (SerializationException) {
                return null;
            }
        }

        private static QmlLanguageServerInstallation? ToInstallation(InstallationManifestDto? dto)
        {
            if (dto is null)
                return null;

            if (string.IsNullOrWhiteSpace(dto.Version)
                || string.IsNullOrWhiteSpace(dto.ReleaseId)
                || string.IsNullOrWhiteSpace(dto.InstallDirectory)
                || string.IsNullOrWhiteSpace(dto.ExecutablePath)
                || string.IsNullOrWhiteSpace(dto.AssetName)
                || string.IsNullOrWhiteSpace(dto.DownloadUrl)
                || string.IsNullOrWhiteSpace(dto.Sha256Digest)) {
                return null;
            }

            return new QmlLanguageServerInstallation(
                dto.Version!,
                dto.ReleaseId!,
                dto.InstallDirectory!,
                dto.ExecutablePath!,
                dto.AssetName!,
                dto.DownloadUrl!,
                dto.Sha256Digest!,
                dto.InstalledAtUtc ?? DateTimeOffset.MinValue);
        }

        private static async Task WriteInstallationManifestAsync(
            QmlLanguageServerInstallation installation,
            string installDirectory,
            CancellationToken cancellationToken)
        {
            var manifestPath = QmlLanguageServerPaths.GetInstallationManifestPath(installDirectory);
            await WriteManifestAsync(installation, manifestPath, cancellationToken);
        }

        private async Task WriteCurrentInstallationManifestAsync(
            QmlLanguageServerInstallation installation,
            CancellationToken ct)
        {
            Directory.CreateDirectory(rootDirectory);
            await WriteManifestAsync(installation,
                QmlLanguageServerPaths.GetCurrentManifestPath(rootDirectory), ct);
        }

        private static async Task WriteManifestAsync(
            QmlLanguageServerInstallation installation,
            string manifestPath,
            CancellationToken cancellationToken)
        {
            var manifest = new InstallationManifestDto
            {
                Version = installation.Version,
                ReleaseId = installation.ReleaseId,
                InstallDirectory = installation.InstallDirectory,
                ExecutablePath = installation.ExecutablePath,
                AssetName = installation.AssetName,
                DownloadUrl = installation.DownloadUrl,
                Sha256Digest = installation.Sha256Digest,
                InstalledAtUtc = installation.InstalledAtUtc,
                LastCheckedAtUtc = DateTimeOffset.UtcNow
            };

            using var manifestStream = new FileStream(
                manifestPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            var serializer = new DataContractJsonSerializer(typeof(InstallationManifestDto));
            serializer.WriteObject(manifestStream, manifest);
            await manifestStream.FlushAsync(cancellationToken);
        }

        private static string ToHexString(IReadOnlyCollection<byte> bytes)
        {
            var chars = new char[bytes.Count * 2];
            var index = 0;
            foreach (var value in bytes) {
                chars[index++] = GetHexCharacter(value >> 4);
                chars[index++] = GetHexCharacter(value & 0xF);
            }

            return new string(chars);
        }

        private static char GetHexCharacter(int value) =>
            (char)(value < 10 ? '0' + value : 'a' + (value - 10));

        [DataContract]
        private sealed class InstallationManifestDto
        {
            [DataMember(Name = "version")]
            public string? Version { get; set; }

            [DataMember(Name = "releaseId")]
            public string? ReleaseId { get; set; }

            [DataMember(Name = "installDirectory")]
            public string? InstallDirectory { get; set; }

            [DataMember(Name = "executablePath")]
            public string? ExecutablePath { get; set; }

            [DataMember(Name = "assetName")]
            public string? AssetName { get; set; }

            [DataMember(Name = "downloadUrl")]
            public string? DownloadUrl { get; set; }

            [DataMember(Name = "sha256Digest")]
            public string? Sha256Digest { get; set; }

            [DataMember(Name = "installedAtUtc")]
            public DateTimeOffset? InstalledAtUtc { get; set; }

            [DataMember(Name = "lastCheckedAtUtc")]
            public DateTimeOffset? LastCheckedAtUtc { get; set; }
        }
    }
}
