// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    [TestClass]
    public class Test_QmlLanguageServerInstaller
    {
        public TestContext TestContext { get; set; }

        private CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;

        private sealed class FakeReleaseClient(Func<QmlLanguageServerRelease> getRelease)
            : IReleaseMetadataClient
        {
            public int Calls { get; private set; }

            public Task<QmlLanguageServerRelease> GetLatestReleaseAsync(CancellationToken ct)
            {
                ++Calls;
                return Task.FromResult(getRelease());
            }
        }

        private sealed class UnusedExtractor : IZipArchiveExtractor
        {
            public Task ExtractAsync(
                string archivePath,
                string targetDirectory,
                CancellationToken ct)
                => throw new InvalidOperationException("Not expected to extract.");
        }

        private static FakeReleaseClient Offline() =>
            new(() => throw new HttpRequestException("No network."));

        // HttpClient reports a timeout as a cancellation that no caller asked for.
        private static FakeReleaseClient TimingOut() =>
            new(() => throw new TaskCanceledException("The request timed out."));

        // HttpClient rejects the scheme before any network access.
        private static FakeReleaseClient ReleaseWithUnreachableAsset()
        {
            return new FakeReleaseClient(() => new QmlLanguageServerRelease("2", "v2",
                "https://example.invalid",
                null,
                DateTimeOffset.UtcNow,
                new QmlLanguageServerAsset("qmlls.zip", "ftp://example.invalid/qmlls.zip", "00")));
        }

        // An installation of v1 whose release was last checked the given time ago.
        private static string InstallPrevious(string root, TimeSpan checkedAgo)
        {
            var installDir = Path.Combine(root, "versions", "v1");
            Directory.CreateDirectory(installDir);
            var executable = Path.Combine(installDir, "qmlls.exe");
            File.WriteAllText(executable, "");
            var lastChecked = DateTimeOffset.UtcNow - checkedAgo;
            File.WriteAllText(Path.Combine(root, "current-installation.json"), $$"""
                {
                  "version": "v1",
                  "releaseId": "1",
                  "installDirectory": {{Json(installDir)}},
                  "executablePath": {{Json(executable)}},
                  "assetName": "qmlls.zip",
                  "downloadUrl": "https://example.invalid/qmlls.zip",
                  "sha256Digest": "00",
                  "installedAtUtc": {{Json(lastChecked)}},
                  "lastCheckedAtUtc": {{Json(lastChecked)}}
                }
                """);
            return executable;

            static string Json<T>(T value)
            {
                using var stream = new MemoryStream();
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T))
                    .WriteObject(stream, value);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        [TestMethod]
        public async Task Uses_Recent_Installation_Without_Checking_For_A_Release()
        {
            using var root = new TempDir();
            var executable = InstallPrevious(root.Path, TimeSpan.FromHours(1));
            var client = Offline();

            var installation = await new QmlLanguageServerInstaller(client, new UnusedExtractor(),
                root.Path).EnsureInstalledAsync(CancellationToken);

            Assert.AreEqual(executable, installation.ExecutablePath);
            Assert.IsNull(installation.UpdateError);
            Assert.AreEqual(0, client.Calls);
        }

        [TestMethod]
        public async Task Uses_Last_Installation_When_The_Release_Is_Unavailable()
        {
            using var root = new TempDir();
            var executable = InstallPrevious(root.Path, TimeSpan.FromDays(30));
            var client = Offline();
            var installer = new QmlLanguageServerInstaller(client, new UnusedExtractor(),
                root.Path);

            var installation = await installer.EnsureInstalledAsync(CancellationToken);
            await installer.EnsureInstalledAsync(CancellationToken);

            Assert.AreEqual(executable, installation.ExecutablePath);
            Assert.AreEqual(QmlLanguageServerInstallError.ReleaseMetadataUnavailable,
                installation.UpdateError?.Error);
            Assert.AreEqual(2, client.Calls, "The next start checks for a release again.");
        }

        [TestMethod]
        public async Task Uses_Last_Installation_When_The_Download_Fails()
        {
            using var root = new TempDir();
            var executable = InstallPrevious(root.Path, TimeSpan.FromDays(30));

            var installation = await new QmlLanguageServerInstaller(ReleaseWithUnreachableAsset(),
                new UnusedExtractor(), root.Path).EnsureInstalledAsync(CancellationToken);

            Assert.AreEqual(executable, installation.ExecutablePath);
            Assert.AreEqual(QmlLanguageServerInstallError.DownloadFailed,
                installation.UpdateError?.Error);
        }

        [TestMethod]
        public async Task Uses_Last_Installation_When_The_Request_Times_Out()
        {
            using var root = new TempDir();
            var executable = InstallPrevious(root.Path, TimeSpan.FromDays(30));

            var installation = await new QmlLanguageServerInstaller(TimingOut(),
                new UnusedExtractor(), root.Path).EnsureInstalledAsync(CancellationToken);

            Assert.AreEqual(executable, installation.ExecutablePath);
            Assert.AreEqual(QmlLanguageServerInstallError.ReleaseMetadataUnavailable,
                installation.UpdateError?.Error);
        }

        [TestMethod]
        public async Task Passes_On_A_Cancellation_By_The_Caller()
        {
            using var root = new TempDir();
            InstallPrevious(root.Path, TimeSpan.FromDays(30));
            using var cancellation = new CancellationTokenSource();
            var client = new FakeReleaseClient(() => {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            });

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                new QmlLanguageServerInstaller(client, new UnusedExtractor(), root.Path)
                    .EnsureInstalledAsync(cancellation.Token));
        }

        [TestMethod]
        public async Task Fails_Without_A_Previous_Installation()
        {
            using var root = new TempDir();

            var ex = await Assert.ThrowsExactlyAsync<QmlLanguageServerInstallException>(() =>
            {
                return new QmlLanguageServerInstaller(Offline(), new UnusedExtractor(), root.Path)
                    .EnsureInstalledAsync(CancellationToken);
            });

            Assert.AreEqual(QmlLanguageServerInstallError.ReleaseMetadataUnavailable, ex.Error);
        }

        [TestMethod]
        public async Task Fails_When_The_Previous_Executable_Is_Gone()
        {
            using var root = new TempDir();
            File.Delete(InstallPrevious(root.Path, TimeSpan.FromDays(30)));

            var ex = await Assert.ThrowsExactlyAsync<QmlLanguageServerInstallException>(() =>
            {
                return new QmlLanguageServerInstaller(Offline(), new UnusedExtractor(), root.Path)
                    .EnsureInstalledAsync(CancellationToken);
            });

            Assert.AreEqual(QmlLanguageServerInstallError.ReleaseMetadataUnavailable, ex.Error);
        }
    }
}
