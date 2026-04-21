# QmlLanguageServer - QML Language Server Installation

## Purpose

The `QmlLanguageServer` namespace is responsible for ensuring the QML Language Server
executable (`qmlls`) is present and up to date on the developer's machine. It fetches release
metadata from the Qt release cache, selects the correct platform-specific binary, downloads
and SHA-256 verifies the archive, extracts it to a per-user install directory, and writes
manifests so subsequent extension startups can skip the network entirely when nothing has
changed.

---

## Design Principles

**Three-level caching minimises network traffic.**
Every call to `EnsureInstalledAsync` walks three decision points before touching the network:

1. If a `current-installation.json` manifest exists and is less than 24 hours old, and the
   recorded executable is still present on disk, return immediately - no network, no disk scan.
2. If the manifest is outdated, fetch release metadata. If a version directory already exists for
   the latest release and its `installation.json` matches the release exactly (version, release
   ID, asset name, SHA-256), return without downloading again.
3. Only if neither condition holds is a full download performed.

This means a developer who builds multiple times a day pays the network cost at most once per
day, and only ever downloads a new archive when a new release is actually available.

**Staging directory prevents corrupt installs.**
The installer never writes directly into the target version directory. Instead it extracts the
archive into a temporary staging directory named `<version>.tmp-<guid>`. Only after extraction
succeeds and the SHA-256 digest has been verified is the staging directory atomically renamed
to the final version directory. If anything fails - network error, digest mismatch, extraction
error - the staging directory is deleted in a `finally` block and the previous installation (if
any) is left untouched.

**SHA-256 verification is mandatory.**
Every downloaded archive is verified against the digest published in the release metadata
before extraction begins. The digest is streamed through `SHA256` without loading the whole
file into memory. A mismatch throws `InvalidDataException` and the archive is discarded.

**Zip path traversal protection on every entry.**
`ZipArchiveExtractor` resolves each archive entry's destination path to its full absolute form
and checks that it starts with the extraction root (with a trailing separator) before writing.
Any entry that would escape the extraction root throws `InvalidDataException` and aborts the
extraction.

**A single semaphore prevents concurrent installs.**
`QmlLanguageServerInstaller` holds a `SemaphoreSlim(1,1)`. If two components call
`EnsureInstalledAsync` concurrently the second call waits until the first completes, then
reads the manifest the first call left behind and returns without repeating the work.

**Platform and architecture selection is centralised.**
`QmlLanguageServerPaths` translates `RuntimeInformation` into the asset name prefix expected
by the release (`qmllanguageserver-<platform>-<architecture>-`). macOS always uses the
`universal` architecture; Windows and Linux resolve to `x64` or `arm64`. Asset selection in
`ReleaseMetadataClient` then filters the release asset list by this prefix and requires exactly
one match - zero or more than one is treated as an error.

**Interface-driven composition.**
`IReleaseMetadataClient`, `IZipArchiveExtractor`, and `IQmlLanguageServerInstaller` define
the contracts. Concrete types are `sealed`. Each component can be replaced independently in
tests without standing up a real HTTP server or file system.

**Failures are typed, not silent.**
The namespace replaces ambiguous `null` returns and bare framework exceptions with three
exception types that pinpoint exactly what went wrong. Callers can catch a specific type and
map it to a user-facing message without inspecting message strings or relying on exception
hierarchy guesswork.

---

## Components

### `QmlLanguageServerInstallException` / `QmlLanguageServerInstallError`

Thrown by `EnsureInstalledAsync` when any step of the install pipeline fails. The `Error`
property is a `QmlLanguageServerInstallError` enum value that identifies the stage:

| Value | Stage |
|---|---|
| `ReleaseMetadataUnavailable` | Network fetch of the release endpoint failed. |
| `NoMatchingAsset` | The release contained zero or more than one asset for the current platform. |
| `DownloadFailed` | The binary archive download failed after the retry. |
| `DigestMismatch` | The downloaded archive's SHA-256 did not match the release metadata. |
| `ExtractionFailed` | The zip extraction failed (e.g. zip-slip check or I/O error). |
| `ExecutableNotFound` | The archive was extracted but no known executable name was found inside. |
| `ManifestWriteFailed` | The installation succeeded but the manifest could not be written. |
| `InstallDirectoryAccessDenied` | The install or staging directory could not be created or written. |

Optional context properties (`InstallDirectory`, `StagingDirectory`, `AssetName`,
`DownloadUrl`) are populated where relevant so a diagnostic log message can include the
specific path or asset that caused the failure.

---

### `QmlLanguageServerAssetException`

Thrown by `ReleaseMetadataClient` when the release endpoint returns a release whose asset
list contains no match for the current platform, or contains ambiguous matches. It is kept
distinct from network and parse failures so `QmlLanguageServerInstaller` can catch it
specifically and map it to `QmlLanguageServerInstallError.NoMatchingAsset` without
referencing implementation internals.

---

### `QmlLanguageServerLaunchException`

Thrown by the extension's `QmlLanguageServerProvider` when the qmlls process fails to start
after a successful installation. Carries `ExecutablePath` so the error message can include
the exact path that was attempted.

---

### `QmlLanguageServerRelease` and `QmlLanguageServerAsset`

Immutable models of a release and its selected platform asset. `QmlLanguageServerRelease`
carries the release identity (`ReleaseId`, `TagName`, `HtmlUrl`, `PublishedAt`) and a single
`QmlLanguageServerAsset` - the one that matched the current platform. `QmlLanguageServerAsset`
carries the asset's file name, download URL, and SHA-256 digest.

---

### `QmlLanguageServerInstallation`

An immutable record of a successfully completed installation. It captures everything needed
both to launch the executable (`ExecutablePath`) and to diagnose problems (`Version`,
`ReleaseId`, `AssetName`, `DownloadUrl`, `Sha256Digest`, `InstalledAtUtc`). This is the type
returned by `EnsureInstalledAsync` and handed to the language server startup logic.

---

### `QmlLanguageServerPaths` (internal)

Centralises all path conventions for the per-user install tree rooted at
`%LocalAppData%\QtBridge\QmlLanguageServer\`:

```
%LocalAppData%\QtBridge\QmlLanguageServer\
  current-installation.json         <- active install + last-checked timestamp
  versions\
    <tag>\
      installation.json             <- permanent manifest for this version
      qmllanguageserver.exe         <- (or qmlls.exe depending on the release)
```

Also provides `GetExpectedAssetPrefix()` (the platform/architecture prefix used for asset
selection) and `GetCandidateExecutableNames()` (the executable names the installer searches
for after extraction - the release ships as `qmllanguageserver` but earlier versions used
`qmlls`).

---

### `IReleaseMetadataClient` / `ReleaseMetadataClient`

Fetches the latest release from `https://qtccache.qt.io/QMLLS/LatestRelease`, which mirrors
a GitHub release JSON structure. Parses the response with a private `DataContract` DTO layer.
Filters the asset list to the single entry matching the current platform prefix, validates the
digest field format (`sha256:<64 hex chars>`), and returns a `QmlLanguageServerRelease`.

Retries the request once after a one-second delay on `HttpRequestException` to handle
transient network hiccups, then propagates on the second failure. The HTTP timeout is 10
seconds - short, since this is metadata, not a binary download.

---

### `IZipArchiveExtractor` / `ZipArchiveExtractor`

Extracts zip archives entry-by-entry, asynchronously streaming each file to disk. Applies
zip slip protection on every entry (described above). Preserves last-write timestamps from
the archive. Directories recorded as entries (empty `Name`) are created without writing a
file.

---

### `IQmlLanguageServerInstaller` / `QmlLanguageServerInstaller`

The orchestrator. `EnsureInstalledAsync` runs the three-level cache check (described above),
and on a cache miss performs the full install sequence:

```
1. DownloadArchiveAsync                   <- streams archive to a temp file in %TEMP%
2. VerifyArchiveDigestAsync               <- SHA-256, streaming; throws on mismatch
3. ExtractAsync                           <- to a staging directory (<version>.tmp-<guid>)
4. Directory.Move                         <- atomic rename staging -> version directory
5. WriteInstallationManifestAsync         <- installation.json inside version directory
6. WriteCurrentInstallationManifestAsync  <- current-installation.json at root
```

The download uses a 60-second HTTP timeout and streams directly to disk with an 80 KB
buffer rather than buffering in memory. Both the temp download file and the staging directory
are deleted in `finally` blocks regardless of outcome.

---

## Typical Call Flow

```
Extension starts (or build completes or language server is needed)
  │
IQmlLanguageServerInstaller.EnsureInstalledAsync()
  │
  |- current-installation.json fresh (< 24h) and executable exists?
  │    -> yes: return cached QmlLanguageServerInstallation immediately
  │
  |- IReleaseMetadataClient.GetLatestReleaseAsync()
  │    -> fetch https://qtccache.qt.io/QMLLS/LatestRelease
  │       select platform asset, parse digest
  │       -> QmlLanguageServerRelease
  │
  |- versions\<tag>\installation.json matches release?
  │    -> yes: update current-installation.json, return existing installation
  │
  |- InstallReleaseAsync()
       DownloadArchiveAsync             -> %TEMP%\<random>.zip
       VerifyArchiveDigestAsync         -> SHA-256 check
       ExtractAsync                     -> versions\<tag>.tmp-<guid>\
       Directory.Move                   -> versions\<tag>\
       WriteInstallationManifestAsync   -> versions\<tag>\installation.json
       WriteCurrentManifestAsync        -> current-installation.json
       -> QmlLanguageServerInstallation
```
