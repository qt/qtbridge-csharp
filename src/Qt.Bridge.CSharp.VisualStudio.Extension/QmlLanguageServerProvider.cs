// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    [VisualStudioContribution]
    internal sealed class QmlLanguageServerProvider : LanguageServerProvider
    {
        private readonly TraceSource logger;
        private readonly IQtBridgeProjectService projectService;
        private readonly IProjectContextService contextService;
        private readonly IQmlMetadataReader metadataReader;
        private readonly IQmlMetadataWatcher metadataWatcher;
        private readonly IQmlLanguageServerInstaller languageServerInstaller;
        private readonly IDisposable contextSubscription;

        private readonly object metadataWatcherLock = new();
        private IDisposable? metadataWatchSubscription;
        private bool metadataWatcherDisposed;

        // True when qmlls is running with minimal args (no metadata file found at startup).
        // OnMetadataChanged uses this to log the upgrade and trigger a restart.
        private volatile bool minimalMode;

        public QmlLanguageServerProvider(
            ExtensionCore container,
            VisualStudioExtensibility extensibilityObject,
            TraceSource traceSource,
            IQtBridgeProjectService projectSvc,
            IProjectContextService contextSvc,
            IQmlMetadataReader metadataReader,
            IQmlMetadataWatcher metadataWatcher,
            IQmlLanguageServerInstaller languageServerInstaller)
            : base(container, extensibilityObject)
        {
            logger = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
            logger.Listeners.Add(new DefaultTraceListener());
            logger.Switch.Level = SourceLevels.Verbose;
            projectService = projectSvc ?? throw new ArgumentNullException(nameof(projectSvc));
            contextService = contextSvc ?? throw new ArgumentNullException(nameof(contextSvc));
            this.metadataReader = metadataReader
                ?? throw new ArgumentNullException(nameof(metadataReader));
            this.metadataWatcher = metadataWatcher
                ?? throw new ArgumentNullException(nameof(metadataWatcher));
            this.languageServerInstaller = languageServerInstaller
                ?? throw new ArgumentNullException(nameof(languageServerInstaller));

            contextSubscription = contextSvc.SubscribeToContextChanged(
                () => _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory
                    .RunAsync(RefreshEnabledStateAsync));

            _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(
                RefreshEnabledStateAsync);
        }

        public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration =>
            new("%QtBridge.QmlLanguageServer.DisplayName%",
            [
                DocumentFilter.FromDocumentType(QmlDocumentTypes.Qml)
            ]);

        public override async Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken ct)
        {
            var (projectDirectory, projectFilePath, configKey) =
                await ResolveActiveProjectContextAsync(ct);
            if (projectDirectory == null || projectFilePath == null || configKey == null) {
                logger.TraceEvent(TraceEventType.Warning, 0,
                    "QML Language Server: no active Qt Bridge project"
                    + " or build configuration found.");
                return null;
            }

            QmlLanguageServerInstallation installation;
            try {
                installation = await languageServerInstaller.EnsureInstalledAsync(ct);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                logger.TraceEvent(TraceEventType.Error, 0,
                    "QML Language Server: failed to acquire executable:"
                    + $" {ex.Message}");
                return null;
            }

            var configuration = Path.GetFileName(configKey);
            var metadataFilePath = metadataReader.FindMetadataFilePath(projectDirectory, configKey);
            if (metadataFilePath == null) {
                logger.TraceEvent(TraceEventType.Information, 0,
                    "QML Language Server: metadata file not found, starting with minimal"
                    + " configuration. Build the project for full QML language support.");
                minimalMode = true;
                return LaunchQmlLanguageServer(installation.ExecutablePath, metadata: null);
            }

            var metadata = metadataReader.TryRead(metadataFilePath);
            if (metadata == null) {
                logger.TraceEvent(TraceEventType.Warning, 0,
                    $"QML Language Server: failed to read metadata at '{metadataFilePath}'.");
                return null;
            }

            if (!metadataReader.Validate(metadata, projectFilePath, configuration)) {
                logger.TraceEvent(TraceEventType.Warning, 0,
                    $"QML Language Server: metadata validation failed for '{metadataFilePath}'.");
                return null;
            }

            minimalMode = false;
            return LaunchQmlLanguageServer(installation.ExecutablePath, metadata);
        }

        public override Task OnServerInitializationResultAsync(
            ServerInitializationResult startState,
            LanguageServerInitializationFailureInfo? initializationFailureInfo,
            CancellationToken cancellationToken)
        {
            if (startState == ServerInitializationResult.Failed) {
                Enabled = false;
                logger.TraceEvent(
                    TraceEventType.Warning,
                    0,
                    initializationFailureInfo?.StatusMessage
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
                ReplaceMetadataWatcher(null, disposing: true);
            }

            base.Dispose(disposing);
        }

        private IDuplexPipe? LaunchQmlLanguageServer(string executablePath, QmlMetadata? metadata)
        {
            var args = metadata != null
                ? BuildQmlLanguageServerArguments(metadata)
                : "--no-cmake-calls";
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
                    logger.TraceEvent(TraceEventType.Error, 0,
                        $"QML Language Server: failed to start '{executablePath}'.");
                    process.Dispose();
                    return null;
                }

                logger.TraceEvent(TraceEventType.Information, 0,
                    $"QML Language Server: started process (pid {process.Id}) with: {args}");

                return new QmlLanguageServerTransportPipe(
                    process,
                    logger,
                    metadata?.Qml.ProjectSourceDir,
                    metadata?.Qml.BuildDirs ?? []);
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                logger.TraceEvent(TraceEventType.Error, 0,
                    $"QML Language Server: exception launching executable: {ex.Message}");
                if (process == null)
                    return null;
                try {
                    if (!process.HasExited)
                        process.Kill();
                } catch (Exception) {}

                process.Dispose();
                return null;
            }
        }

        private static string BuildQmlLanguageServerArguments(QmlMetadata metadata)
        {
            var parts = new List<string>();

            if (metadata.QmlLanguageServer.DisableCMakeCalls)
                parts.Add("--no-cmake-calls");

            parts.AddRange(metadata.Qml.BuildDirs.Select(d => $"-b \"{d}\""));
            parts.AddRange(metadata.Qml.ImportPaths.Select(p => $"-I \"{p}\""));

            return string.Join(" ", parts);
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

        private async Task RefreshEnabledStateAsync()
        {
            var shouldEnable = await ShouldEnableForActiveContextAsync(CancellationToken.None);

            if (shouldEnable)
                await UpdateMetadataWatcherAsync(CancellationToken.None);
            else
                StopMetadataWatcher();

            if (Enabled == shouldEnable)
                return;

            Enabled = shouldEnable;
            logger.TraceEvent(
                TraceEventType.Information,
                0,
                shouldEnable
                    ? "Enabled QML Language Server provider for Qt Bridge context."
                    : "Disabled QML Language Server provider for Qt Bridge context.");
        }

        private async Task UpdateMetadataWatcherAsync(CancellationToken ct)
        {
            var (projectDirectory, _, configKey) = await ResolveActiveProjectContextAsync(ct);
            if (projectDirectory == null || string.IsNullOrWhiteSpace(configKey))
                return;

            var next = metadataWatcher.Watch(projectDirectory, configKey!, OnMetadataChanged);
            ReplaceMetadataWatcher(next);
        }

        private void StopMetadataWatcher() => ReplaceMetadataWatcher(null);

        private void ReplaceMetadataWatcher(IDisposable? next, bool disposing = false)
        {
            IDisposable? previous;
            lock (metadataWatcherLock) {
                if (metadataWatcherDisposed) {
                    previous = next;
                } else {
                    if (disposing)
                        metadataWatcherDisposed = true;
                    previous = metadataWatchSubscription;
                    metadataWatchSubscription = next;
                }
            }
            previous?.Dispose();
        }

        private void OnMetadataChanged()
        {
            _ = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    if (!Enabled) {
                        // Server is not running (e.g., first startup was cancelled before
                        // the metadata file existed). Try re-enabling now that the metadata
                        // has appeared or changed. RefreshEnabledStateAsync is a no-op when
                        // the context has no Qt Bridge project.
                        await RefreshEnabledStateAsync();
                        return;
                    }

                    logger.TraceEvent(TraceEventType.Information, 0,
                        minimalMode
                            ? "QML Language Server: metadata file appeared after build,"
                                + " restarting with full configuration."
                            : "QML Language Server: metadata file changed, restarting server.");

                    // Disable first to signal the SDK to shut down the current server,
                    // then re-enable to trigger a fresh CreateServerConnectionAsync call.
                    // The delay gives the SDK time to clean up the previous connection.
                    // TODO: verify this matches the LanguageServerProvider SDK contract.
                    Enabled = false;
                    await Task.Delay(500);
                    Enabled = true;
                });
        }
    }
}
