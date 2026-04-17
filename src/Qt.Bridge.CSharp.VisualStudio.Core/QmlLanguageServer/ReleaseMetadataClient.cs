// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer
{
    /// <summary>
    /// Fetches and parses QML Language Server release metadata from the Qt release cache
    /// endpoint, selecting the asset that matches the current platform and architecture.
    /// </summary>
    public sealed class ReleaseMetadataClient : IReleaseMetadataClient
    {
        private const string LatestReleaseUrl = "https://qtccache.qt.io/QMLLS/LatestRelease";
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public async Task<QmlLanguageServerRelease> GetLatestReleaseAsync(CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++) {
                try {
                    return await FetchAndParseAsync(ct);
                } catch (HttpRequestException) when (attempt == 0) {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
            }
        }

        private static async Task<QmlLanguageServerRelease> FetchAndParseAsync(CancellationToken ct)
        {
            using var response = await HttpClient.GetAsync(LatestReleaseUrl, ct);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            var serializer = new DataContractJsonSerializer(typeof(GitHubReleaseDto));
            if (serializer.ReadObject(responseStream) is not GitHubReleaseDto releaseDto)
                throw new InvalidDataException(
                    "Failed to parse QML Language Server release metadata.");

            if (string.IsNullOrWhiteSpace(releaseDto.TagName))
                throw new InvalidDataException("LatestRelease payload did not include a release tag.");

            var tagName = releaseDto.TagName!;

            var expectedAssetPrefix = QmlLanguageServerPaths.GetExpectedAssetPrefix();
            var matchingAssets = (releaseDto.Assets ?? [])
                .Where(asset => asset.Name != null
                    && asset.Name.StartsWith(expectedAssetPrefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchingAssets.Length == 0) {
                throw new InvalidOperationException(
                    $"Latest QML Language Server release '{tagName}' did not contain"
                    + $" a supported asset matching '{expectedAssetPrefix}*'.");
            }

            if (matchingAssets.Length != 1) {
                throw new InvalidOperationException(
                    $"Latest QML Language Server release '{tagName}' contained multiple"
                    + $" supported assets matching '{expectedAssetPrefix}*'.");
            }

            var asset = matchingAssets[0];
            var digest = ParseSha256Digest(asset.Digest);

            return new QmlLanguageServerRelease(
                releaseDto.Id?.ToString()
                    ?? throw new InvalidDataException("Release payload did not include an id."),
                tagName,
                releaseDto.HtmlUrl ?? "",
                releaseDto.Body,
                ParsePublishedAt(releaseDto.PublishedAt),
                new QmlLanguageServerAsset(
                    asset.Name
                        ?? throw new InvalidDataException("Release asset did not include a name."),
                    asset.BrowserDownloadUrl
                        ?? throw new InvalidDataException("Release asset did not include a URL."),
                    digest));
        }

        private static string ParseSha256Digest(string? digest)
        {
            if (digest == null || string.IsNullOrWhiteSpace(digest))
                throw new InvalidDataException("Release asset did not include a SHA-256 digest.");

            const string prefix = "sha256:";
            if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Unsupported release asset digest format: {digest}");

            var hexDigest = digest.Substring(prefix.Length);
            if (hexDigest.Length != 64)
                throw new InvalidDataException($"Invalid SHA-256 digest length: {digest}");

            return hexDigest.ToLowerInvariant();
        }

        private static DateTimeOffset ParsePublishedAt(string? publishedAt)
        {
            if (string.IsNullOrWhiteSpace(publishedAt))
                return DateTimeOffset.MinValue;

            return DateTimeOffset.TryParse(
                publishedAt,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var value)
                ? value
                : DateTimeOffset.MinValue;
        }

        [DataContract]
        private sealed class GitHubReleaseDto
        {
            [DataMember(Name = "id")]
            public long? Id { get; set; }

            [DataMember(Name = "tag_name")]
            public string? TagName { get; set; }

            [DataMember(Name = "html_url")]
            public string? HtmlUrl { get; set; }

            [DataMember(Name = "body")]
            public string? Body { get; set; }

            [DataMember(Name = "published_at")]
            public string? PublishedAt { get; set; }

            [DataMember(Name = "assets")]
            public GitHubAssetDto[]? Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAssetDto
        {
            [DataMember(Name = "name")]
            public string? Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }

            [DataMember(Name = "digest")]
            public string? Digest { get; set; }
        }
    }
}
