// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

using CoreQmlMetadata = Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata.QmlMetadata;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    [VisualStudioContribution]
    internal sealed class QmlLanguageServerProvider : LanguageServerProvider
    {
        private readonly IExtensionLog log;
        private readonly INotificationService notifications;
        private readonly IQtBridgeProjectService projectService;
        private readonly IProjectContextService contextService;
        private readonly IQmlMetadataReader metadataReader;
        private readonly IQmlMetadataWatcher metadataWatcher;
        private readonly IQmlLanguageServerInstaller languageServerInstaller;
        private readonly IDisposable contextSubscription;

        private sealed class ProjectEntry(
            IDisposable watcher,
            string projectDirectory,
            string configKey)
        {
            public IDisposable Watcher { get; } = watcher;
            public string ProjectDirectory { get; } = projectDirectory;
            public string ConfigKey { get; } = configKey;
            public bool BuildDirsInjected { get; set; }
            public bool MissingMetadataNotified { get; set; }
            // Set after metadata changes. Once .qmlls.build.ini is present and patched,
            // restart qmlls so it can read the updated build settings in a fresh process.
            public bool RestartWhenIniReady { get; set; }
            // Non-null while a FileSystemWatcher is waiting for .qt/.qmlls.build.ini to appear
            // or finish changing. Prevents duplicate watchers for the same project.
            public IDisposable? IniWatcher { get; set; }
        }

        private readonly object registryLock = new();
        private readonly Dictionary<string, ProjectEntry> projectRegistry = [];
        private bool registryDisposed;
        private QmlLanguageServerTransportPipe? activePipe;

        public QmlLanguageServerProvider(
            ExtensionCore container,
            VisualStudioExtensibility extensibilityObject,
            IExtensionLog extensionLog,
            INotificationService notificationsSvc,
            IQtBridgeProjectService projectSvc,
            IProjectContextService contextSvc,
            IQmlMetadataReader metadataReader,
            IQmlMetadataWatcher metadataWatcher,
            IQmlLanguageServerInstaller languageServerInstaller)
            : base(container, extensibilityObject)
        {
            log = extensionLog
                ?? throw new ArgumentNullException(nameof(extensionLog));
            notifications = notificationsSvc
                ?? throw new ArgumentNullException(nameof(notificationsSvc));
            projectService = projectSvc ?? throw new ArgumentNullException(nameof(projectSvc));
            contextService = contextSvc ?? throw new ArgumentNullException(nameof(contextSvc));
            this.metadataReader = metadataReader
                ?? throw new ArgumentNullException(nameof(metadataReader));
            this.metadataWatcher = metadataWatcher
                ?? throw new ArgumentNullException(nameof(metadataWatcher));
            this.languageServerInstaller = languageServerInstaller
                ?? throw new ArgumentNullException(nameof(languageServerInstaller));

            contextSubscription = contextSvc.SubscribeToContextChanged(
                () => QueueLoggedTask(
                    RefreshEnabledStateAsync,
                    "refresh QML Language Server provider state"));

            QueueLoggedTask(RefreshEnabledStateAsync, "initial QML Language Server provider state");
        }

        public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration =>
            new("%QtBridge.QmlLanguageServer.DisplayName%",
            [
                DocumentFilter.FromDocumentType(QmlDocumentTypes.Qml)
            ]);

        public override async Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken ct)
        {
            QmlLanguageServerInstallation installation;
            try {
                installation = await languageServerInstaller.EnsureInstalledAsync(ct);
            } catch (QmlLanguageServerInstallException ex) {
                log.Error($"QML Language Server: install failed ({ex.Error}).", ex);
                var userMessage = InstallErrorMessage(ex.Error);
                await notifications.ShowErrorAsync($"qmls-install-{ex.Error}", userMessage, ct);
                return null;
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                log.Error("QML Language Server: unexpected error acquiring executable.", ex);
                return null;
            }

            // Best-effort: read import paths from any loaded Qt Bridge project. These paths are
            // identical for all projects using the same NuGet package version, so the first project
            // with built metadata is a valid source even if the active project has none yet.
            var importPaths = await TryFindImportPathsAsync(ct);
            if (importPaths is string[] logImportPaths) {
                foreach (var importPath in logImportPaths)
                    log.Info($"QML Language Server: startup import path: {importPath}");
            } else {
                log.Info("QML Language Server: startup import-path resolution found no paths.");
            }
            var (activeDir, activeFile, activeKey) =
                await ResolveActiveProjectContextAsync(ct);

            QmlLanguageServerTransportPipe pipe;
            try {
                pipe = LaunchQmlLanguageServer(installation.ExecutablePath, importPaths);
            } catch (QmlLanguageServerLaunchException ex) {
                log.Error($"QML Language Server: failed to launch '{ex.ExecutablePath}'.", ex);
                await notifications.ShowErrorAsync("qmls-launch-failed",
                    "Qt Bridge: The QML Language Server failed to start."
                    + " See the Qt Bridge output pane for details.", ct);
                return null;
            }

            List<string> existingProjects;
            lock (registryLock) {
                activePipe = pipe;
                foreach (var entry in projectRegistry.Values)
                    entry.BuildDirsInjected = false;
                existingProjects = [..projectRegistry.Keys];
            }

            // Update all previously registered projects so the new server session has full
            // workspace and build-directory context for every project the user has visited.
            foreach (var projectPath in existingProjects)
                await TryInjectProjectAsync(projectPath, ct, notifyUser: false);

            // Register and inject the active project (may already be in the registry).
            if (activeDir != null && activeFile != null && activeKey != null)
                await EnsureProjectRegisteredAsync(
                    activeFile,
                    activeDir,
                    activeKey,
                    ct,
                    notifyUser: true);

            return pipe;
        }

        public override Task OnServerInitializationResultAsync(
            ServerInitializationResult startState,
            LanguageServerInitializationFailureInfo? initializationFailureInfo,
            CancellationToken cancellationToken)
        {
            if (startState == ServerInitializationResult.Failed) {
                Enabled = false;
                log.Warning(initializationFailureInfo?.StatusMessage
                    ?? "QML Language Server initialization failed.");
            }

            return base.OnServerInitializationResultAsync(
                startState,
                initializationFailureInfo,
                cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                contextSubscription.Dispose();
                activePipe = null;
                lock (registryLock) {
                    registryDisposed = true;
                    foreach (var entry in projectRegistry.Values) {
                        entry.Watcher.Dispose();
                        entry.IniWatcher?.Dispose();
                    }
                    projectRegistry.Clear();
                }
            }

            base.Dispose(disposing);
        }

        private void QueueLoggedTask(Func<Task> action, string operationName)
        {
            _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(() =>
                RunLoggedAsync(action, operationName));
        }

        private async Task RunLoggedAsync(Func<Task> action, string operationName)
        {
            try {
                await action();
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                log.Error($"{operationName} failed.", ex);
#if DEBUG
                if (Debugger.IsAttached)
                    Debugger.Break();
#endif
            }
        }

        private QmlLanguageServerTransportPipe LaunchQmlLanguageServer(
            string executablePath,
            IEnumerable<string>? importPaths)
        {
            var args = BuildStartupArguments(importPaths);
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process? process = null;
            try {
                process = new Process { StartInfo = startInfo };
                if (!process.Start()) {
                    process.Dispose();
                    throw new QmlLanguageServerLaunchException("Process.Start() returned false.",
                        executablePath);
                }

                log.Info($"QML Language Server: started process (pid {process.Id}) with: {args}");

                return new QmlLanguageServerTransportPipe(process, log);
            } catch (QmlLanguageServerLaunchException) {
                throw;
            } catch (Exception ex) {
                if (process == null) {
                    throw new QmlLanguageServerLaunchException("Exception while launching process.",
                        executablePath, ex);
                }

                try {
                    if (!process.HasExited)
                        process.Kill();
                } catch (Exception) {}

                process.Dispose();
                throw new QmlLanguageServerLaunchException("Exception while launching process.",
                    executablePath, ex);
            }
        }

        private static string BuildStartupArguments(IEnumerable<string>? importPaths)
        {
            var parts = new List<string> { "--no-cmake-calls" };
            if (importPaths != null)
                parts.AddRange(importPaths.Select(p => $"-I \"{p}\""));
            return string.Join(" ", parts);
        }

        private async Task<IEnumerable<string>?> TryFindImportPathsAsync(CancellationToken ct)
        {
            var configuration = await contextService.GetActiveConfigurationAsync(ct);
            if (string.IsNullOrWhiteSpace(configuration))
                return null;

            var platform = await contextService.GetActivePlatformAsync(ct);
            var isRealPlatform = !string.IsNullOrWhiteSpace(platform)
                && !string.Equals(platform, "Any CPU", StringComparison.OrdinalIgnoreCase);
            var configKey = isRealPlatform
                ? Path.Combine(platform!, configuration!)
                : configuration!;

            var importPaths = new List<string>();
            var loadedPaths = await contextService.GetLoadedProjectPathsAsync(ct);
            foreach (var projectPath in loadedPaths) {
                var meta = await projectService.TryGetMetadataForPathAsync(projectPath, ct);
                if (meta?.IsQtBridgeProject != true)
                    continue;

                var packageImportPath = TryResolveNuGetQmlImportPath(projectPath, meta);
                if (!string.IsNullOrWhiteSpace(packageImportPath)
                    && Directory.Exists(packageImportPath)
                    && !importPaths.Contains(packageImportPath, StringComparer.OrdinalIgnoreCase)) {
                    importPaths.Add(packageImportPath!);
                }

                var projectDir = Path.GetDirectoryName(projectPath);
                if (projectDir == null)
                    continue;

                var metadataPath = metadataReader.FindMetadataFilePath(projectDir, configKey);
                if (metadataPath == null)
                    continue;

                var readResult = metadataReader.TryRead(metadataPath, ct);
                if (!readResult.Success || readResult.Metadata!.Qml.ImportPaths.Count == 0)
                    continue;

                foreach (var importPath in readResult.Metadata.Qml.ImportPaths) {
                    if (!string.IsNullOrWhiteSpace(importPath)
                        && !importPaths.Contains(importPath, StringComparer.OrdinalIgnoreCase)) {
                        importPaths.Add(importPath);
                    }
                }
            }
            return importPaths.Count > 0 ? importPaths : null;
        }

        private string? TryResolveNuGetQmlImportPath(
            string projectFilePath,
            QtBridgeProjectMetadata metadata)
        {
            var packageId = metadata.MatchedPackageId;
            if (string.IsNullOrWhiteSpace(projectFilePath))
                return null;

            var projectDirectory = Path.GetDirectoryName(projectFilePath);
            if (string.IsNullOrWhiteSpace(projectDirectory))
                return null;

            var assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
            if (!File.Exists(assetsPath))
                return null;

            ProjectAssetsDto? assets;
            try {
                using var stream = File.OpenRead(assetsPath);
                var serializer = new DataContractJsonSerializer(
                    typeof(ProjectAssetsDto),
                    new DataContractJsonSerializerSettings {
                        UseSimpleDictionaryFormat = true
                    });
                assets = serializer.ReadObject(stream) as ProjectAssetsDto;
            } catch (Exception ex) {
                log.Warning($"QML Language Server: failed to read project assets '{assetsPath}': "
                    + ex.Message);
                return null;
            }

            if (assets?.Libraries == null || assets.PackageFolders == null)
                return null;

            if (string.IsNullOrWhiteSpace(packageId)) {
                var inferredPackageEntry = assets.Libraries
                    .FirstOrDefault(entry => QtBridgeProjectConstants.KnownPackageIdPrefixes.Any(
                        prefix => entry.Key.StartsWith(prefix,
                            StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(inferredPackageEntry.Key))
                    packageId = inferredPackageEntry.Key.Split('/')[0];
                else
                    return null;
            }

            var packageEntry = assets.Libraries
                .FirstOrDefault(entry => entry.Key.StartsWith(packageId + "/",
                    StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(packageEntry.Key)
                || string.IsNullOrWhiteSpace(packageEntry.Value?.Path)) {
                return null;
            }

            foreach (var packageFolder in assets.PackageFolders.Keys) {
                if (string.IsNullOrWhiteSpace(packageFolder))
                    continue;
                var importPath = Path.Combine(packageFolder, packageEntry.Value!.Path!, "tools",
                    "qt", "qml");
                if (Directory.Exists(importPath))
                    return importPath;
            }

            return null;
        }

        private async Task EnsureProjectRegisteredAsync(
            string projectFilePath,
            string projectDirectory,
            string configKey,
            CancellationToken ct,
            bool notifyUser = false)
        {
            bool needsWatcher;
            lock (registryLock) {
                if (registryDisposed)
                    return;
                needsWatcher = !projectRegistry.TryGetValue(projectFilePath, out var existing)
                    || existing.ConfigKey != configKey;
            }

            if (needsWatcher) {
                var watcher = metadataWatcher.Watch(
                    projectDirectory, configKey,
                    () => OnProjectMetadataChanged(projectFilePath));

                IDisposable? displaced = null;
                IDisposable? displacedIniWatcher = null;
                lock (registryLock) {
                    if (registryDisposed) {
                        watcher.Dispose();
                        return;
                    }

                    if (projectRegistry.TryGetValue(projectFilePath, out var existing)
                            && existing.ConfigKey == configKey) {
                        watcher.Dispose(); // another thread already registered this config
                    } else {
                        if (projectRegistry.TryGetValue(projectFilePath, out existing)) {
                            displaced = existing.Watcher; // config changed, displace old watcher
                            displacedIniWatcher = existing.IniWatcher;
                        }
                        projectRegistry[projectFilePath] =
                            new ProjectEntry(watcher, projectDirectory, configKey);
                    }
                }
                displaced?.Dispose();
                displacedIniWatcher?.Dispose();
            }

            await TryInjectProjectAsync(projectFilePath, ct, notifyUser);
        }

        private async Task TryInjectProjectAsync(
            string projectFilePath,
            CancellationToken ct,
            bool notifyUser = false)
        {
            QmlLanguageServerTransportPipe? pipe;
            string? projectDirectory, configKey;
            lock (registryLock) {
                pipe = activePipe;
                if (pipe == null)
                    return;
                if (!projectRegistry.TryGetValue(projectFilePath, out var e))
                    return;
                if (e.BuildDirsInjected)
                    return;
                // Claim the slot now so a concurrent call that also read false above cannot
                // proceed to a duplicate injection while we are awaiting async work below.
                e.BuildDirsInjected = true;
                projectDirectory = e.ProjectDirectory;
                configKey = e.ConfigKey;
            }

            // Local helper: release the claim so a future call can retry.
            void ResetInjection()
            {
                lock (registryLock) {
                    if (projectRegistry.TryGetValue(projectFilePath, out var e))
                        e.BuildDirsInjected = false;
                }
            }

            var metadataFilePath = metadataReader.FindMetadataFilePath(projectDirectory, configKey);
            if (metadataFilePath == null) {
                var shouldNotify = false;
                lock (registryLock) {
                    if (projectRegistry.TryGetValue(projectFilePath, out var e)) {
                        e.BuildDirsInjected = false;
                        shouldNotify = notifyUser && !e.MissingMetadataNotified;
                        if (shouldNotify)
                            e.MissingMetadataNotified = true;
                    }
                }

                if (!shouldNotify)
                    return;

                var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                await notifications.ShowInfoAsync($"qmls-no-metadata:{projectFilePath}",
                    $"Qt Bridge: Build project '{projectName}' for full QML language support.", ct);
                return;
            }

            var readResult = metadataReader.TryRead(metadataFilePath, ct);
            if (!readResult.Success) {
                ResetInjection();
                log.Error($"QML Language Server: failed to read metadata at '{readResult.Path}'"
                    + $" ({readResult.Error}).", readResult.Exception);
                return;
            }

            var configuration = Path.GetFileName(configKey);
            if (!metadataReader.Validate(readResult.Metadata!, projectFilePath, configuration)) {
                ResetInjection();
                return;
            }

            var metadata = readResult.Metadata!;
            var sourceDir = metadata.Qml.ProjectSourceDir;
            var buildDirs = metadata.Qml.BuildDirs;

            if (string.IsNullOrEmpty(sourceDir) || buildDirs.Count == 0) {
                ResetInjection();
                log.Warning(
                    $"QML Language Server: metadata for '{Path.GetFileName(projectFilePath)}'"
                    + " has no source dir or build dirs - skipping injection.");
                return;
            }

            // qmlls selects .qmlls.build.ini settings by matching the file path against the
            // section header (startsWith check). The generated section is keyed by the native
            // source root (e.g. obj/.../qt/native/source), which does not match user-authored
            // QML files under the project root. Add an alias section for the project root so
            // qmlls can resolve project-specific types (e.g. C#-exposed types) in those files.
            //
            // The .qmlls.build.ini is generated during the native build, which can complete after
            // the metadata JSON that triggered this injection attempt. Do not send $/addBuildDirs
            // until the ini exists and has been patched; qmlls memoizes build-dir settings and
            // will not revisit an already-seen build path in the current session.
            if (!TryPatchQmllsBuildIni(metadata, projectFilePath)) {
                ResetInjection();
                log.Info($"QML Language Server: metadata not fully ready for"
                    + $" '{Path.GetFileName(projectFilePath)}' - delaying injection until"
                    + " .qmlls.build.ini exists.");
                EnsureBuildSettingsWatcher(projectFilePath, projectDirectory, notifyUser);
                return;
            }

            if (!TryGeneratedQmlTypesReady(metadata, projectFilePath)) {
                ResetInjection();
                log.Info($"QML Language Server: metadata not fully ready for"
                    + $" '{Path.GetFileName(projectFilePath)}' - delaying injection until"
                    + " generated .qmltypes files exist.");
                EnsureBuildSettingsWatcher(projectFilePath, projectDirectory, notifyUser);
                return;
            }

            var shouldRestart = false;
            lock (registryLock) {
                if (projectRegistry.TryGetValue(projectFilePath, out var entry)
                    && entry.RestartWhenIniReady) {
                    entry.RestartWhenIniReady = false;
                    entry.BuildDirsInjected = false;
                    entry.IniWatcher?.Dispose();
                    entry.IniWatcher = null;
                    shouldRestart = true;
                }
            }
            if (shouldRestart) {
                log.Info($"QML Language Server: .qmlls.build.ini is ready for"
                    + $" '{Path.GetFileName(projectFilePath)}' - restarting qmlls with"
                    + " a clean build-settings cache.");
                await RestartServerForProjectAsync(projectFilePath);
                return;
            }

            var folderUri = new Uri(sourceDir).AbsoluteUri;
            log.Info($"QML Language Server: registering workspace folder for"
                + $" '{Path.GetFileName(projectFilePath)}' (uri={folderUri}).");
            pipe.EnqueueNotification(
                QmlLanguageServerTransportPipe.BuildWorkspaceFolderNotification(
                    folderUri, add: true));
            log.Info($"QML Language Server: injecting $/addBuildDirs for"
                + $" '{Path.GetFileName(projectFilePath)}' ({buildDirs.Count} build dir(s)).");
            pipe.EnqueueNotification(
                QmlLanguageServerTransportPipe.BuildAddBuildDirsNotification(folderUri, buildDirs));

            // If activePipe changed while we were working the notifications went to a dead
            // pipe. Reset so CreateServerConnectionAsync rehydrates on the new session.
            lock (registryLock) {
                if (!ReferenceEquals(activePipe, pipe)) {
                    if (projectRegistry.TryGetValue(projectFilePath, out var e))
                        e.BuildDirsInjected = false;
                    log.Info($"QML Language Server: pipe replaced during injection of"
                        + $" '{Path.GetFileName(projectFilePath)}' - will retry on new session.");
                    return;
                }
                if (projectRegistry.TryGetValue(projectFilePath, out var entry)) {
                    entry.MissingMetadataNotified = false;
                    entry.IniWatcher?.Dispose();
                    entry.IniWatcher = null;
                }
            }

            log.Info($"QML Language Server: injected build dirs for"
                + $" '{Path.GetFileName(projectFilePath)}'.");
        }

        // Returns true if the ini file was found for at least one build dir (and was patched or
        // already had the alias). Returns false if any required ini file is missing or could not
        // be patched yet.
        private bool TryPatchQmllsBuildIni(CoreQmlMetadata metadata, string projectFilePath)
        {
            var projectSourceDir = Path.GetDirectoryName(projectFilePath)
                ?? metadata.Qml.ProjectSourceDir;
            if (string.IsNullOrEmpty(projectSourceDir)) {
                log.Info("QML Language Server: .qmlls.build.ini patch skipped - no projectSourceDir.");
                return true; // treat as "done" - nothing to patch without a project source dir
            }

            var nativeKey = BuildQmllsBuildIniSectionKey(metadata.Qml.SourceDir);
            var aliasKey = BuildQmllsBuildIniSectionKey(projectSourceDir!);

            log.Info($"QML Language Server: .qmlls.build.ini patch - nativeKey={nativeKey}"
                + $" aliasKey={aliasKey}");

            if (string.Equals(nativeKey, aliasKey, StringComparison.OrdinalIgnoreCase)) {
                log.Info("QML Language Server: .qmlls.build.ini patch skipped - keys are equal.");
                return true;
            }

            var anyFound = false;
            var allReady = true;
            foreach (var buildDir in metadata.Qml.BuildDirs) {
                var iniPath = Path.Combine(buildDir, ".qt", ".qmlls.build.ini");
                if (!File.Exists(iniPath)) {
                    log.Info($"QML Language Server: .qmlls.build.ini not found at '{iniPath}'.");
                    allReady = false;
                    continue;
                }
                anyFound = true;
                try {
                    if (!AppendQmllsBuildIniAlias(iniPath, nativeKey, aliasKey,
                        Path.GetFileNameWithoutExtension(projectFilePath))) {
                        allReady = false;
                    }
                } catch (Exception ex) when (ex is not OperationCanceledException) {
                    log.Warning($"QML Language Server: failed to patch '{iniPath}': {ex.Message}");
                    allReady = false;
                }
            }
            return anyFound && allReady;
        }

        private static string BuildQmllsBuildIniSectionKey(string path)
        {
            var normalized = path
                .Replace('\\', '/')
                .TrimEnd('/');
            if (normalized.Length >= 2 && normalized[1] == ':')
                normalized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
            return "[" + normalized.Replace("/", "<SLASH>") + "]";
        }

        private bool TryGeneratedQmlTypesReady(CoreQmlMetadata metadata, string projectFilePath)
        {
            var buildDirs = metadata.Qml.BuildDirs
                .Select(Path.GetFullPath)
                .ToArray();
            var generatedImportPaths = metadata.Qml.ImportPaths
                .Where(path => buildDirs.Any(buildDir => IsPathUnder(path, buildDir)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (generatedImportPaths.Length == 0)
                return true;

            foreach (var importPath in generatedImportPaths) {
                if (!Directory.Exists(importPath)) {
                    log.Info($"QML Language Server: generated import path not found"
                        + $" for '{Path.GetFileName(projectFilePath)}': '{importPath}'.");
                    return false;
                }

                var qmlTypes = Directory.EnumerateFiles(
                    importPath, "*.qmltypes", SearchOption.TopDirectoryOnly).ToArray();
                if (qmlTypes.Length == 0) {
                    log.Info($"QML Language Server: no generated .qmltypes files found"
                        + $" for '{Path.GetFileName(projectFilePath)}' in '{importPath}'.");
                    return false;
                }

                foreach (var qmlTypesPath in qmlTypes) {
                    try {
                        using var _ = new FileStream(
                            qmlTypesPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read);
                    } catch (IOException ex) {
                        log.Info($"QML Language Server: generated .qmltypes file is not ready"
                            + $" for '{Path.GetFileName(projectFilePath)}':"
                            + $" '{qmlTypesPath}' ({ex.Message}).");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsPathUnder(string path, string root)
        {
            var fullPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var fullRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureBuildSettingsWatcher(
            string projectFilePath,
            string projectDirectory,
            bool notifyUser)
        {
            FileSystemWatcher? watcher;
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return;
                if (entry.IniWatcher != null)
                    return;

                watcher = new FileSystemWatcher(projectDirectory, "*.*")
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.LastWrite
                        | NotifyFilters.CreationTime
                };
                entry.IniWatcher = watcher;
            }

            void QueueRetry(string reason)
            {
                log.Info($"QML Language Server: detected {reason} for"
                    + $" '{Path.GetFileName(projectFilePath)}' - retrying injection.");
                QueueLoggedTask(
                    () => TryInjectProjectAsync(
                        projectFilePath,
                        CancellationToken.None,
                        notifyUser),
                    $"retry injection after build settings {reason} for"
                    + $" {Path.GetFileName(projectFilePath)}");
            }

            watcher.Created += (_, e) => QueueRetry($"new build file '{e.Name}'");
            watcher.Changed += (_, e) => QueueRetry($"build file change '{e.Name}'");
            watcher.Renamed += (_, e) => QueueRetry($"build file rename '{e.Name}'");
            watcher.Error += (_, e) =>
            {
                log.Warning($"QML Language Server: build settings watcher failed for"
                    + $" '{Path.GetFileName(projectFilePath)}': {e.GetException().Message}");
            };
            watcher.EnableRaisingEvents = true;

            log.Info($"QML Language Server: watching '{projectDirectory}'"
                + " for .qmlls.build.ini and generated .qmltypes files.");
        }

        private async Task RestartServerForProjectAsync(string projectFilePath)
        {
            if (!Enabled)
                return;

            Enabled = false;
            await Task.Delay(500);
            Enabled = true;
            log.Info($"QML Language Server: restarted after"
                + $" '{Path.GetFileName(projectFilePath)}' ini became ready.");
        }

        private bool AppendQmllsBuildIniAlias(
            string iniPath,
            string nativeKey,
            string aliasKey,
            string projectName)
        {
            var lines = File.ReadAllLines(iniPath);

            if (lines.Any(l => string.Equals(
                l.Trim(), aliasKey, StringComparison.OrdinalIgnoreCase))) {
                log.Info($"QML Language Server: .qmlls.build.ini already has alias '{aliasKey}'.");
                return true;
            }

            // Collect the key=value lines from the native section.
            var inNative = false;
            var sectionLines = new List<string>();
            foreach (var line in lines) {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]")) {
                    if (inNative) break;
                    inNative = string.Equals(
                        trimmed, nativeKey, StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inNative && trimmed.Length > 0)
                    sectionLines.Add(line);
            }

            if (sectionLines.Count == 0) {
                log.Info($"QML Language Server: .qmlls.build.ini patch skipped - native section"
                    + $" '{nativeKey}' not found in '{iniPath}' ({lines.Length} lines).");
                return false;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(aliasKey);
            foreach (var kv in sectionLines)
                sb.AppendLine(kv);

            var existing = File.ReadAllText(iniPath);
            var toAppend = sb.ToString();
            if (existing.Length > 0 && existing[existing.Length - 1] != '\n')
                toAppend = Environment.NewLine + toAppend;

            File.AppendAllText(iniPath, toAppend);
            log.Info($"QML Language Server: patched .qmlls.build.ini for '{projectName}'"
                + $" - added alias '{aliasKey}'.");
            return true;
        }

        private void OnProjectMetadataChanged(string projectFilePath)
        {
            QueueLoggedTask(async () =>
            {
                if (!Enabled) {
                    await RefreshEnabledStateAsync();
                    return;
                }

                lock (registryLock) {
                    if (!projectRegistry.TryGetValue(projectFilePath, out var e))
                        return;
                    e.BuildDirsInjected = false;
                    e.RestartWhenIniReady = true;
                    e.IniWatcher?.Dispose();
                    e.IniWatcher = null;
                }

                // After a build, metadata JSON may appear before the build has finished writing
                // .qmlls.build.ini. Wait until the ini exists and is patched,
                // then restart once so open documents enter the new qmlls session with the
                // correct startup import paths and build-dir settings from the outset.
                log.Info($"QML Language Server: metadata changed for"
                    + $" '{Path.GetFileName(projectFilePath)}', waiting for"
                    + " .qmlls.build.ini before restart.");
                await TryInjectProjectAsync(projectFilePath,
                    CancellationToken.None,
                    notifyUser: false);
            }, $"metadata changed for {Path.GetFileName(projectFilePath)}");
        }

        private async Task RefreshEnabledStateAsync()
        {
            var ct = CancellationToken.None;
            var shouldEnable = await ShouldEnableForActiveContextAsync(ct);

            if (shouldEnable) {
                // Register the active project immediately for fast first-open response.
                var (dir, file, key) = await ResolveActiveProjectContextAsync(ct);
                if (dir != null && file != null && key != null)
                    await EnsureProjectRegisteredAsync(file, dir, key, ct, notifyUser: true);

                // Register all other loaded Qt Bridge projects so the server has full solution
                // coverage without requiring the user to visit each project first.
                // configKey is solution-wide (platform\configuration), resolved once.
                var configuration = await contextService.GetActiveConfigurationAsync(ct);
                var platform = await contextService.GetActivePlatformAsync(ct);
                if (!string.IsNullOrWhiteSpace(configuration)) {
                    var isRealPlatform = !string.IsNullOrWhiteSpace(platform)
                        && !string.Equals(platform, "Any CPU", StringComparison.OrdinalIgnoreCase);
                    var configKey = isRealPlatform
                        ? Path.Combine(platform!, configuration!)
                        : configuration!;

                    var loadedPaths = await contextService.GetLoadedProjectPathsAsync(ct);
                    foreach (var projectPath in loadedPaths) {
                        var meta = await projectService
                            .TryGetMetadataForPathAsync(projectPath, ct);
                        if (meta?.IsQtBridgeProject != true) continue;
                        var projectDir = Path.GetDirectoryName(projectPath);
                        if (projectDir == null) continue;
                        await EnsureProjectRegisteredAsync(
                            projectPath,
                            projectDir,
                            configKey,
                            ct,
                            notifyUser: false);
                    }
                }
            }

            if (Enabled == shouldEnable)
                return;

            Enabled = shouldEnable;
            log.Info(shouldEnable
                ? "Enabled QML Language Server provider for Qt Bridge context."
                : "Disabled QML Language Server provider for Qt Bridge context.");
        }

        /// <summary>
        /// Resolves the active project context to a (projectDirectory, projectFilePath,
        /// configKey) triple for server startup, where <c>configKey</c> is
        /// <c>platform\configuration</c> for real platform names (e.g. <c>x64\Debug</c>),
        /// or just <c>configuration</c> for <c>Any CPU</c> and unspecified platforms
        /// (matching the plain <c>obj\Debug\</c> layout).
        /// <para>
        /// When an active document exists it must belong to a Qt Bridge project - if it
        /// does not, <c>(null, null, null)</c> is returned immediately and the selected
        /// project is never consulted. This enforces the design requirement that only
        /// <c>.qml</c> files belonging to a Qt Bridge project are routed to this server.
        /// The selected-project fallback is only used when there is no active document
        /// context (e.g. when setting up the metadata watcher from Solution Explorer).
        /// </para>
        /// </summary>
        private async Task<(string? dir, string? file, string? configKey)>
            ResolveActiveProjectContextAsync(CancellationToken ct)
        {
            var configuration = await contextService.GetActiveConfigurationAsync(ct);
            if (string.IsNullOrWhiteSpace(configuration))
                return (null, null, null);

            var platform = await contextService.GetActivePlatformAsync(ct);

            // Any CPU means MSBuild omits the platform segment from BaseIntermediateOutputPath,
            // producing obj\Debug\ rather than obj\Any CPU\Debug\.
            var isRealPlatform = !string.IsNullOrWhiteSpace(platform)
                && !string.Equals(platform, "Any CPU", StringComparison.OrdinalIgnoreCase);
            var configKey = isRealPlatform
                ? Path.Combine(platform!, configuration!)
                : configuration!;

            // (1) Active document - must belong to a Qt Bridge project.
            //     If a document is active but is not owned by a Qt Bridge project, return
            //     null immediately rather than falling through to the selected project.
            //     This prevents routing unrelated .qml files to a Qt Bridge server.
            var activeDocument = await contextService.GetActiveDocumentPathAsync(ct);
            if (!string.IsNullOrWhiteSpace(activeDocument)) {
                var owningPath = await contextService.GetOwningProjectPathAsync(activeDocument!, ct);
                if (owningPath == null)
                    return (null, null, null);

                var meta = await projectService.TryGetMetadataForPathAsync(owningPath, ct);
                if (meta?.IsQtBridgeProject != true)
                    return (null, null, null);

                var dir = Path.GetDirectoryName(owningPath);
                return dir != null ? (dir, owningPath, configKey) : (null, null, null);
            }

            // (2) No active document - use the explicitly selected project as proxy.
            //     Covers watcher setup from Solution Explorer when no file is open.
            var activeProject = await contextService.GetActiveProjectPathAsync(ct);
            if (string.IsNullOrWhiteSpace(activeProject))
                return (null, null, null);

            var activeMeta = await projectService.TryGetMetadataForPathAsync(activeProject!, ct);
            if (activeMeta?.IsQtBridgeProject != true)
                return (null, null, null);

            var activeDir = Path.GetDirectoryName(activeProject);
            return activeDir != null ? (activeDir, activeProject, configKey) : (null, null, null);
        }

        private async Task<bool> ShouldEnableForActiveContextAsync(CancellationToken ct)
        {
            var activeProjectPath = await contextService.GetActiveProjectPathAsync(ct);
            if (await IsQtBridgeProjectAsync(activeProjectPath, ct))
                return true;

            var activeDocumentPath = await contextService.GetActiveDocumentPathAsync(ct);
            if (await IsQtBridgeProjectAsync(activeDocumentPath, ct))
                return true;

            var loadedProjectPaths = await contextService.GetLoadedProjectPathsAsync(ct);
            foreach (var projectPath in loadedProjectPaths) {
                if (await IsQtBridgeProjectAsync(projectPath, ct))
                    return true;
            }

            return false;
        }

        private async Task<bool> IsQtBridgeProjectAsync(string? path, CancellationToken ct)
        {
            if (path == null || string.IsNullOrWhiteSpace(path))
                return false;

            var metadata = await projectService.TryGetMetadataForPathAsync(path, ct);
            return metadata?.IsQtBridgeProject == true;
        }

        private static string InstallErrorMessage(QmlLanguageServerInstallError err) => err switch
        {
            QmlLanguageServerInstallError.ReleaseMetadataUnavailable =>
                "Qt Bridge: Could not fetch QML Language Server metadata. "
                + "Check your network connection.",
            QmlLanguageServerInstallError.NoMatchingAsset =>
                "Qt Bridge: No QML Language Server package found for this platform.",
            QmlLanguageServerInstallError.DownloadFailed =>
                "Qt Bridge: Could not download the QML Language Server. "
                + "Check your network connection.",
            QmlLanguageServerInstallError.DigestMismatch =>
                "Qt Bridge: The QML Language Server package failed integrity verification "
                + "and was not installed.",
            QmlLanguageServerInstallError.ExtractionFailed =>
                "Qt Bridge: Could not extract the QML Language Server package.",
            QmlLanguageServerInstallError.ExecutableNotFound =>
                "Qt Bridge: QML Language Server executable was not found after install.",
            QmlLanguageServerInstallError.ManifestWriteFailed =>
                "Qt Bridge: Could not save the QML Language Server install manifest.",
            QmlLanguageServerInstallError.InstallDirectoryAccessDenied =>
                "Qt Bridge: Cannot write to the QML Language Server install directory.",
            _ =>
                "Qt Bridge: Could not install the QML Language Server. "
                + "See the Qt Bridge output pane for details."
        };

        [DataContract]
        private sealed class ProjectAssetsDto
        {
            [DataMember(Name = "libraries")]
            public Dictionary<string, ProjectAssetsLibraryDto>? Libraries { get; set; }

            [DataMember(Name = "packageFolders")]
            public Dictionary<string, ProjectAssetsPackageFolderDto>? PackageFolders { get; set; }
        }

        [DataContract]
        private sealed class ProjectAssetsLibraryDto
        {
            [DataMember(Name = "path")]
            public string? Path { get; set; }
        }

        [DataContract]
        private sealed class ProjectAssetsPackageFolderDto;
    }
}
