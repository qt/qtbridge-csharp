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
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

using QmlMetadataModel = Qt.Bridge.CSharp.VisualStudio.Core.QmlMetadata.QmlMetadata;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    [VisualStudioContribution]
    internal sealed class QmlLanguageServerProvider : LanguageServerProvider
    {
        private readonly IExtensionLog log;
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
            IExtensionLog extensionLog,
            IQtBridgeProjectService projectSvc,
            IProjectContextService contextSvc,
            IQmlMetadataReader metadataReader,
            IQmlMetadataWatcher metadataWatcher,
            IQmlLanguageServerInstaller languageServerInstaller)
            : base(container, extensibilityObject)
        {
            log = extensionLog
                ?? throw new ArgumentNullException(nameof(extensionLog));
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
            var (projectDirectory, projectFilePath, configKey) =
                await ResolveActiveProjectContextAsync(ct);
            if (projectDirectory == null || projectFilePath == null || configKey == null) {
                log.Warning("QML Language Server: no active Qt Bridge project or build "
                    + "configuration found.");
                return null;
            }

            QmlLanguageServerInstallation installation;
            try {
                installation = await languageServerInstaller.EnsureInstalledAsync(ct);
            } catch (QmlLanguageServerInstallException ex) {
                log.Error($"QML Language Server: install failed ({ex.Error}).", ex);
                return null;
            } catch (Exception ex) when (ex is not OperationCanceledException) {
                log.Error("QML Language Server: unexpected error acquiring executable.", ex);
                return null;
            }

            var configuration = Path.GetFileName(configKey);
            var metadataFilePath = metadataReader.FindMetadataFilePath(projectDirectory, configKey);
            if (metadataFilePath == null) {
                log.Info("QML Language Server: metadata file not found, starting with minimal"
                    + " configuration. Build the project for full QML language support.");
                minimalMode = true;
                try {
                    return LaunchQmlLanguageServer(installation.ExecutablePath, metadata: null);
                } catch (QmlLanguageServerLaunchException ex) {
                    log.Error($"QML Language Server: failed to launch '{ex.ExecutablePath}'.", ex);
                    return null;
                }
            }

            var readResult = metadataReader.TryRead(metadataFilePath, ct);
            if (!readResult.Success) {
                log.Warning($"QML Language Server: failed to read metadata at '{metadataFilePath}'"
                    + $" ({readResult.Error}).");
                return null;
            }

            if (!metadataReader.Validate(readResult.Metadata!, projectFilePath, configuration)) {
                log.Warning("QML Language Server: metadata validation failed"
                    + $" for '{metadataFilePath}'.");
                return null;
            }

            minimalMode = false;
            try {
                return LaunchQmlLanguageServer(installation.ExecutablePath, readResult.Metadata!);
            } catch (QmlLanguageServerLaunchException ex) {
                log.Error($"QML Language Server: failed to launch '{ex.ExecutablePath}'.", ex);
                return null;
            }
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
                ReplaceMetadataWatcher(null, disposing: true);
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

        private IDuplexPipe LaunchQmlLanguageServer(string executablePath, QmlMetadataModel? metadata)
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
                    process.Dispose();
                    throw new QmlLanguageServerLaunchException("Process.Start() returned false.",
                        executablePath);
                }

                log.Info($"QML Language Server: started process (pid {process.Id}) with: {args}");

                return new QmlLanguageServerTransportPipe(
                    process,
                    log,
                    metadata?.Qml.ProjectSourceDir,
                    metadata?.Qml.BuildDirs ?? []);
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

        private static string BuildQmlLanguageServerArguments(QmlMetadataModel metadata)
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

            log.Info(shouldEnable
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
            QueueLoggedTask(async () =>
            {
                if (!Enabled) {
                    // Server is not running (e.g., first startup was cancelled before
                    // the metadata file existed). Try re-enabling now that the metadata
                    // has appeared or changed.
                    await RefreshEnabledStateAsync();
                    return;
                }

                log.Info(minimalMode
                    ? "QML Language Server: metadata file appeared after build, restarting "
                        + "with full configuration."
                    : "QML Language Server: metadata file changed, restarting server.");

                // Disable first to signal the SDK to shut down the current server,
                // then re-enable to trigger a fresh CreateServerConnectionAsync call.
                // The delay gives the SDK time to clean up the previous connection.
                // TODO: verify this matches the LanguageServerProvider SDK contract.
                Enabled = false;
                await Task.Delay(500);
                Enabled = true;
            }, "metadata changed handler");
        }
    }
}
