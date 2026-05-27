// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization.Json;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer.Contracts;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

using CoreQmlMetadata = Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata.QmlMetadata;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    [VisualStudioContribution]
    internal sealed partial class QmlLanguageServerProvider : LanguageServerProvider
    {
        private readonly IExtensionLog log;
        private readonly INotificationService notifications;
        private readonly IQtBridgeProjectService projectService;
        private readonly IProjectContextService contextService;
        private readonly IQmlMetadataReader metadataReader;
        private readonly IQmlMetadataWatcher metadataWatcher;
        private readonly IQmlLanguageServerInstaller languageServerInstaller;
        private readonly ILoggingSettingsProvider loggingSettingsProvider;
        private readonly IQmlBuildNotificationSettings buildNotificationSettings;
        private readonly IDisposable contextSubscription;
        private readonly QmllsBuildIniPatcher buildIniPatcher;

        private static string MissingMetadataNotificationKey(string projectFilePath) =>
            $"qmls-no-metadata:{projectFilePath}";

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
            // Set after build settings change. Once .qmlls.build.ini is present and patched,
            // restart qmlls so it can read the updated build settings in a fresh process.
            public bool RestartWhenIniReady { get; set; }
            // Polls generated qmlls build settings. The files are build outputs and can be
            // rewritten by rebuild, clean+build, or command-line builds.
            public BuildSettingsMonitor? BuildSettingsMonitor { get; set; }
        }

        private readonly record struct FileSignature(
            bool Exists,
            DateTime LastWriteTimeUtc,
            long Length);

        private sealed class BuildSettingsMonitor : IDisposable
        {
            private readonly CancellationTokenSource cts = new();
            private Dictionary<string, FileSignature>? signatures;

            public CancellationToken Token => cts.Token;

            // Returns true after the initial snapshot when a monitored file signature changes.
            public bool UpdateSignatures(Dictionary<string, FileSignature> current)
            {
                if (signatures == null) {
                    signatures = current;
                    return false; // First-time initialization: no update
                }

                if (signatures.Count != current.Count) {
                    signatures = current;
                    return true; // Counts differ: update occurred
                }

                foreach (var entry in signatures) {
                    if (current.TryGetValue(entry.Key, out var value) && value.Equals(entry.Value))
                        continue;
                    signatures = current;
                    return true; // Value mismatch: update occurred
                }

                return false; // No differences found
            }

            public void Dispose()
            {
                cts.Cancel();
                cts.Dispose();
            }
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
            IQmlLanguageServerInstaller languageServerInstaller,
            ILoggingSettingsProvider loggingSettingsProvider,
            IQmlBuildNotificationSettings buildNotificationSettings)
            : base(container, extensibilityObject)
        {
            log = extensionLog
                ?? throw new ArgumentNullException(nameof(extensionLog));
            buildIniPatcher = new QmllsBuildIniPatcher(log);
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
            this.loggingSettingsProvider = loggingSettingsProvider
                ?? throw new ArgumentNullException(nameof(loggingSettingsProvider));
            this.buildNotificationSettings = buildNotificationSettings
                ?? throw new ArgumentNullException(nameof(buildNotificationSettings));

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
            if (importPaths != null) {
                foreach (var importPath in importPaths)
                    log.Info($"QML Language Server: startup import path: {importPath}");
            } else {
                log.Info("QML Language Server: startup import-path resolution found no paths.");
            }
            var (activeDir, activeFile, activeKey) =
                await ResolveActiveProjectContextAsync(ct);

            var loggingConfig = await ReadLoggingConfigAsync(ct);

            QmlLanguageServerTransportPipe pipe;
            try {
                pipe = LaunchQmlLanguageServer(installation.ExecutablePath, importPaths,
                    loggingConfig);
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
            } else {
                QueueSemanticTokensRefresh("server initialization");
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
                        entry.BuildSettingsMonitor?.Dispose();
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
            IEnumerable<string>? importPaths,
            LoggingOptions loggingOptions)
        {
            var qmllsLogActive = loggingOptions.QmllsLogEnabled
                && !string.IsNullOrWhiteSpace(loggingOptions.QmllsLogFilePath);
            if (qmllsLogActive)
                ResetQmlLanguageServerLogOnce(loggingOptions.QmllsLogFilePath);

            var args = BuildStartupArguments(importPaths, qmllsLogActive,
                loggingOptions.QmllsLogFilePath);
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

                return new QmlLanguageServerTransportPipe(process, log, loggingOptions);
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

        private static string BuildStartupArguments(
            IEnumerable<string>? importPaths,
            bool qmllsLogActive,
            string qmllsLogFilePath)
        {
            var parts = new List<string> { "--no-cmake-calls" };
            if (qmllsLogActive)
                parts.AddRange(["--verbose", $"-l \"{qmllsLogFilePath}\""]);
            if (importPaths != null)
                parts.AddRange(importPaths.Select(p => $"-I \"{p}\""));
            return string.Join(" ", parts);
        }

        private async Task<string[]?> TryFindImportPathsAsync(CancellationToken ct)
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
            var importPathKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loadedPaths = await contextService.GetLoadedProjectPathsAsync(ct);
            foreach (var projectPath in loadedPaths) {
                var meta = await projectService.TryGetMetadataForPathAsync(projectPath, ct);
                if (meta?.IsQtBridgeProject != true)
                    continue;

                var packageImportPath = TryResolveNuGetQmlImportPath(projectPath, meta);
                if (!string.IsNullOrWhiteSpace(packageImportPath)
                    && Directory.Exists(packageImportPath)) {
                    AddStartupImportPath(importPaths, importPathKeys, packageImportPath!);
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
                    if (!string.IsNullOrWhiteSpace(importPath))
                        AddStartupImportPath(importPaths, importPathKeys, importPath);
                }
            }
            return importPaths.Count > 0 ? [..importPaths] : null;
        }

        private static void AddStartupImportPath(
            ICollection<string> importPaths,
            ISet<string> importPathKeys,
            string path)
        {
            if (importPathKeys.Add(BuildStartupImportPathKey(path)))
                importPaths.Add(path);
        }

        private static string BuildStartupImportPathKey(string path)
        {
            try {
                path = Path.GetFullPath(path);
            } catch (Exception ex) when (ex is ArgumentException or NotSupportedException
                or PathTooLongException) {
                // Ignore.
            }

            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
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
                IDisposable? displacedBuildSettingsMonitor = null;
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
                            displacedBuildSettingsMonitor = existing.BuildSettingsMonitor;
                        }
                        projectRegistry[projectFilePath] =
                            new ProjectEntry(watcher, projectDirectory, configKey);
                    }
                }
                displaced?.Dispose();
                displacedBuildSettingsMonitor?.Dispose();
            }

            EnsureBuildSettingsMonitor(projectFilePath);
            await TryInjectProjectAsync(projectFilePath, ct, notifyUser);
        }

        private async Task TryInjectProjectAsync(
            string projectFilePath,
            CancellationToken ct,
            bool notifyUser = false,
            bool logNotReady = true)
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
                var shouldCheckNotificationSettings = false;
                lock (registryLock) {
                    if (projectRegistry.TryGetValue(projectFilePath, out var e)) {
                        e.BuildDirsInjected = false;
                        shouldCheckNotificationSettings = notifyUser && !e.MissingMetadataNotified;
                    }
                }

                if (!shouldCheckNotificationSettings)
                    return;

                var showNotification = await buildNotificationSettings
                    .ShouldShowMissingBuildOutputNotificationAsync(projectFilePath, ct);
                if (!showNotification)
                    return;

                lock (registryLock) {
                    if (!projectRegistry.TryGetValue(projectFilePath, out var e)
                        || e.MissingMetadataNotified) {
                        return;
                    }
                    e.MissingMetadataNotified = true;
                }

                var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                await notifications.ShowInfoAsync(
                    MissingMetadataNotificationKey(projectFilePath),
                    $"Qt Bridge: Build project '{projectName}' for full QML language support.",
                    [
                        new NotificationAction("Don't show for this project", actionCt =>
                            buildNotificationSettings.SuppressMissingBuildOutputNotificationAsync(
                                projectFilePath, actionCt)),
                        new NotificationAction("Disable build notifications", actionCt =>
                            buildNotificationSettings
                                .SetMissingBuildOutputNotificationsEnabledAsync(false, actionCt))
                    ],
                    ct);
                return;
            }

            var readResult = metadataReader.TryRead(metadataFilePath, ct);
            if (!readResult.Success) {
                ResetInjection();
                if (readResult.Error == QmlMetadataReadError.IoError) {
                    log.Info($"QML Language Server: metadata file is not ready for"
                        + $" '{Path.GetFileName(projectFilePath)}' - will retry.");
                    QueueLoggedTask(async () =>
                    {
                        await Task.Delay(250, CancellationToken.None);
                        await TryInjectProjectAsync(projectFilePath, CancellationToken.None,
                            notifyUser, logNotReady);
                    }, $"retry injection after metadata read for {Path.GetFileName(projectFilePath)}");
                } else {
                    log.Error($"QML Language Server: failed to read metadata at '{readResult.Path}'"
                        + $" ({readResult.Error}).", readResult.Exception);
                }
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
            // section header (startsWith check). The generated section is keyed by the generated
            // source root (e.g. obj/.../qt/native/source), which does not match user-authored
            // QML files under the project root. Add an alias section for the project root so
            // qmlls can resolve project-specific types (e.g. C#-exposed types) in those files.
            //
            // The .qmlls.build.ini is generated during the native build, which can complete after
            // the metadata JSON that triggered this injection attempt. Do not send $/addBuildDirs
            // until the ini exists and has been patched; qmlls memoizes build-dir settings and
            // will not revisit an already-seen build path in the current session.
            if (!BuildSettingsIniFilesExist(metadata)) {
                ResetInjection();
                if (logNotReady) {
                    log.Info($"QML Language Server: metadata not fully ready for"
                        + $" '{Path.GetFileName(projectFilePath)}' - delaying injection until"
                        + " .qmlls.build.ini exists.");
                }
                return;
            }

            if (!buildIniPatcher.TryPatch(metadata, projectFilePath)) {
                ResetInjection();
                if (logNotReady) {
                    log.Info($"QML Language Server: metadata not fully ready for"
                        + $" '{Path.GetFileName(projectFilePath)}' - delaying injection until"
                        + " .qmlls.build.ini exists.");
                }
                return;
            }

            if (!TryGeneratedQmlTypesReady(metadata, projectFilePath)) {
                ResetInjection();
                if (logNotReady) {
                    log.Info($"QML Language Server: metadata not fully ready for"
                        + $" '{Path.GetFileName(projectFilePath)}' - delaying injection until"
                        + " generated .qmltypes files exist.");
                }
                return;
            }

            var shouldRestart = false;
            lock (registryLock) {
                if (projectRegistry.TryGetValue(projectFilePath, out var entry)
                    && entry.RestartWhenIniReady) {
                    entry.RestartWhenIniReady = false;
                    entry.BuildDirsInjected = false;
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
                }
            }

            var projectFileName = Path.GetFileName(projectFilePath);
            log.Info($"QML Language Server: injected build dirs for '{projectFileName}'.");

            QueueSemanticTokensRefresh($"build-dir injection for {projectFileName}");
        }

        private void QueueSemanticTokensRefresh(string reason)
        {
            QueueLoggedTask(async () =>
            {
                await Task.Delay(250);
                QmlLanguageServerTransportPipe? pipe;
                lock (registryLock)
                    pipe = activePipe;
                pipe?.EnqueueSemanticTokensRefresh();
                log.Info($"QML Language Server: requested semantic token refresh after {reason}.");
            }, $"semantic token refresh after {reason}");
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
                    log.Verbose($"QML Language Server: generated import path not found"
                        + $" for '{Path.GetFileName(projectFilePath)}': '{importPath}'.");
                    return false;
                }

                var qmlTypes = Directory.EnumerateFiles(
                    importPath, "*.qmltypes", SearchOption.TopDirectoryOnly).ToArray();
                if (qmlTypes.Length == 0) {
                    log.Verbose($"QML Language Server: no generated .qmltypes files found"
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
                        log.Verbose($"QML Language Server: generated .qmltypes file is not ready"
                            + $" for '{Path.GetFileName(projectFilePath)}':"
                            + $" '{qmlTypesPath}' ({ex.Message}).");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool BuildSettingsIniFilesExist(CoreQmlMetadata metadata)
        {
            return metadata.Qml.BuildDirs
                .All(buildDir => File.Exists(Path.Combine(buildDir, ".qt", ".qmlls.build.ini")));
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

        private void EnsureBuildSettingsMonitor(string projectFilePath)
        {
            BuildSettingsMonitor? monitor;
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return;
                if (entry.BuildSettingsMonitor != null)
                    return;

                monitor = new BuildSettingsMonitor();
                entry.BuildSettingsMonitor = monitor;
            }

            log.Verbose($"QML Language Server: monitoring generated build settings for"
                + $" '{Path.GetFileName(projectFilePath)}'.");

            QueueLoggedTask(async () =>
            {
                var monitorToken = monitor.Token;
                while (!monitorToken.IsCancellationRequested) {
                    await Task.Delay(1000, monitorToken);

                    var signatures = GetBuildSettingsSignatures(projectFilePath, monitorToken);
                    var changed = monitor.UpdateSignatures(signatures);

                    bool shouldRetry;
                    var shouldLogChange = false;
                    lock (registryLock) {
                        if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                            return;
                        if (changed && entry.BuildDirsInjected) {
                            entry.BuildDirsInjected = false;
                            entry.RestartWhenIniReady = true;
                            shouldLogChange = true;
                        }
                        shouldRetry = changed || (!entry.BuildDirsInjected && signatures.Count > 0);
                    }

                    if (!shouldRetry)
                        continue;

                    if (shouldLogChange) {
                        log.Info($"QML Language Server: generated build settings changed for"
                            + $" '{Path.GetFileName(projectFilePath)}' - retrying injection.");
                    }
                    await TryInjectProjectAsync(projectFilePath, CancellationToken.None,
                        notifyUser: false,
                        logNotReady: false);
                }
            }, $"monitor QML Language Server build settings for {Path.GetFileName(projectFilePath)}");
        }

        private Dictionary<string, FileSignature> GetBuildSettingsSignatures(
            string projectFilePath,
            CancellationToken ct)
        {
            string? projectDirectory, configKey;
            lock (registryLock) {
                if (!projectRegistry.TryGetValue(projectFilePath, out var entry))
                    return [];
                projectDirectory = entry.ProjectDirectory;
                configKey = entry.ConfigKey;
            }

            var metadataFilePath = metadataReader.FindMetadataFilePath(projectDirectory, configKey);
            if (metadataFilePath == null)
                return [];

            var readResult = metadataReader.TryRead(metadataFilePath, ct);
            if (!readResult.Success)
                return [];

            var configuration = Path.GetFileName(configKey);
            if (!metadataReader.Validate(readResult.Metadata!, projectFilePath, configuration))
                return [];

            var metadata = readResult.Metadata!;
            var signatures = new Dictionary<string, FileSignature>(StringComparer.OrdinalIgnoreCase);
            var buildDirs = metadata.Qml.BuildDirs
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var buildDir in buildDirs) {
                var dir = Path.Combine(buildDir, ".qt");
                AddPathSignature(signatures, Path.Combine(dir, ".qmlls.build.ini"));
                AddPathSignature(signatures, Path.Combine(dir, "qtbridge_project_sources.qrc"));
            }

            var generatedImportPaths = metadata.Qml.ImportPaths
                .Where(path => buildDirs.Any(buildDir => IsPathUnder(path, buildDir)))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var importPath in generatedImportPaths) {
                AddPathSignature(signatures, importPath);
                if (!Directory.Exists(importPath))
                    continue;
                foreach (var qmlTypesPath in Directory.EnumerateFiles(
                    importPath, "*.qmltypes", SearchOption.TopDirectoryOnly)) {
                    AddPathSignature(signatures, qmlTypesPath);
                }
            }

            return signatures;
        }

        private static void AddPathSignature(
            IDictionary<string, FileSignature> signatures,
            string path)
        {
            var normalizedPath = Path.GetFullPath(path);
            try {
                if (File.Exists(normalizedPath)) {
                    var file = new FileInfo(normalizedPath);
                    signatures[normalizedPath] = new FileSignature(true, file.LastWriteTimeUtc,
                        file.Length);
                    return;
                }
                if (Directory.Exists(normalizedPath)) {
                    var directory = new DirectoryInfo(normalizedPath);
                    signatures[normalizedPath] = new FileSignature(true, directory.LastWriteTimeUtc,
                        0);
                    return;
                }
            } catch (IOException) {
                signatures[normalizedPath] = new FileSignature(true, DateTime.MinValue, -1);
                return;
            } catch (UnauthorizedAccessException) {
                signatures[normalizedPath] = new FileSignature(true, DateTime.MinValue, -1);
                return;
            }

            signatures[normalizedPath] = new FileSignature(false, DateTime.MinValue, 0);
        }

        private async Task RestartServerForProjectAsync(string projectFilePath)
        {
            if (!Enabled)
                return;

            lock (registryLock)
                activePipe = null;

            Enabled = false;
            await Task.Delay(500);
            Enabled = true;
            log.Info($"QML Language Server: restarted after"
                + $" '{Path.GetFileName(projectFilePath)}' ini became ready.");
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
            var (shouldEnable, contextReady) = await EvaluateEnabledStateAsync(ct);
            var loadedPaths = await contextService.GetLoadedProjectPathsAsync(ct);
            RefreshProjectRegistry(loadedPaths);

            switch (shouldEnable) {
            case false when !contextReady:
                log.Info("QML Language Server provider state deferred until VS project context "
                    + "is ready.");
                return;
            case true:
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
                break;
            }

            if (Enabled == shouldEnable)
                return;

            Enabled = shouldEnable;
            log.Info(shouldEnable
                ? "Enabled QML Language Server provider for Qt Bridge context."
                : "Disabled QML Language Server provider for Qt Bridge context.");
        }

        private void RefreshProjectRegistry(IEnumerable<string> loadedProjectPaths)
        {
            var loadedProjects = loadedProjectPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<ProjectEntry> removedEntries = [];
            List<string> removedProjectPaths = [];
            lock (registryLock) {
                foreach (var project in projectRegistry.ToList()) {
                    if (loadedProjects.Contains(project.Key))
                        continue;

                    projectRegistry.Remove(project.Key);
                    removedEntries.Add(project.Value);
                    removedProjectPaths.Add(project.Key);
                }
            }

            foreach (var entry in removedEntries) {
                entry.Watcher.Dispose();
                entry.BuildSettingsMonitor?.Dispose();
            }

            foreach (var projectPath in removedProjectPaths)
                notifications.ClearRateLimit(MissingMetadataNotificationKey(projectPath));
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

        private async Task<(bool, bool)> EvaluateEnabledStateAsync(CancellationToken ct)
        {
            var activeProjectPath = await contextService.GetActiveProjectPathAsync(ct);
            if (await IsQtBridgeProjectAsync(activeProjectPath, ct))
                return (true, true);

            var activeDocumentPath = await contextService.GetActiveDocumentPathAsync(ct);
            if (await IsQtBridgeProjectAsync(activeDocumentPath, ct))
                return (true, true);

            var loadedProjectPaths = await contextService.GetLoadedProjectPathsAsync(ct);
            foreach (var projectPath in loadedProjectPaths) {
                if (await IsQtBridgeProjectAsync(projectPath, ct))
                    return (true, true);
            }

            var contextReady = loadedProjectPaths.Count > 0
                || !string.IsNullOrWhiteSpace(activeProjectPath);
            return (false, contextReady);
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
    }
}
