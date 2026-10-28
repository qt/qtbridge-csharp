// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.Notifications;
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings;
#if WINDOWS_DESKTOP
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.Notifications;
#endif
using Qt.Bridge.CSharp.VisualStudio.Extension.Settings.QmlLanguageServer;
using Qt.Bridge.CSharp.VisualStudio.Extension.WhatsNew;

using Task = System.Threading.Tasks.Task;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(QmlLanguageServerOptionsPage), "Qt Bridge for C#",
        "QML Language Server", 0, 0, true, SupportsProfiles = true,
        IsInUnifiedSettings = true)]
#if WINDOWS_DESKTOP
    [ProvideOptionPage(typeof(NotificationOptionsPage), "Qt Bridge for C#",
        "Notifications", 0, 0, true, SupportsProfiles = false,
        IsInUnifiedSettings = true)]
    [ProvideService(typeof(IQmlBuildNotificationSettingsService), IsCacheable = true)]
    [ProvideService(typeof(VsUserNotificationProvider), IsCacheable = true)]
#endif
    [ProvideSettingsManifest(PackageRelativeManifestFile = "QtBridge.registration.json")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string,
        PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    public sealed class ExtensionPackage : AsyncPackage
#if WINDOWS_DESKTOP
        , IOleCommandTarget
#endif
    {
        internal const string PackageGuidString = "D4E5F6A7-B8C9-4D2E-8F1A-3B5C7D9E1F2A";

        internal static QmlLanguageServerOptionsPage? QmlLanguageServerPage { get; private set; }

        public static ExtensionPackage? Instance { get; private set; }

#if WINDOWS_DESKTOP
        private VsUserNotificationProvider? userNotificationProvider;
        private ToolWindowNotificationService? toolWindowNotificationSvc;
        private uint toolWindowNotificationCommandTargetCookie;
        internal IToolWindowNotificationService? ToolWindowNotifications => toolWindowNotificationSvc;
#endif
        internal WhatsNewLifecycleService? WhatsNewLifecycle { get; private set; }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            Instance = this;
#if WINDOWS_DESKTOP
            AddService(typeof(IQmlBuildNotificationSettingsService),
                CreateQmlBuildNotificationExternalSettingsServiceAsync,
                promote: true);
            AddService(typeof(VsUserNotificationProvider),
                CreateVsUserNotificationProviderAsync,
                promote: true);
#endif
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            QmlLanguageServerPage = (QmlLanguageServerOptionsPage)GetDialogPage(
                typeof(QmlLanguageServerOptionsPage));
            VSColorTheme.ThemeChanged += OnVsThemeChanged;
#if WINDOWS_DESKTOP
            await InitializeToolWindowNotificationCommandsAsync(cancellationToken);
#endif
            await InitializeWhatsNewLifecycleAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
#pragma warning disable VSTHRD104
            if (disposing)
                JoinableTaskFactory.Run(DisposeAsync);
#pragma warning restore VSTHRD104

            base.Dispose(disposing);
        }

        private async Task DisposeAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
#if WINDOWS_DESKTOP
            UnregisterToolWindowNotificationCommandTarget();
#endif
            VSColorTheme.ThemeChanged -= OnVsThemeChanged;
        }

        private async Task InitializeWhatsNewLifecycleAsync(CancellationToken cancellationToken)
        {
#if WINDOWS_DESKTOP
            var log = new TraceExtensionLog();
            var toastNotifications = new ToastNotificationService(
                new InfoBarNotificationService(log),
                log);
            EnsureToolWindowNotifications(log);
            WhatsNewLifecycle = new WhatsNewLifecycleService(
                new SettingsStore(this),
                toastNotifications,
                ToolWindowNotifications,
                log);

            await WhatsNewLifecycle.InitializeAsync(cancellationToken);
#else
            await Task.CompletedTask;
#endif
        }

#if WINDOWS_DESKTOP
        private Task<object?> CreateQmlBuildNotificationExternalSettingsServiceAsync(
            IAsyncServiceContainer container,
            CancellationToken cancellationToken,
            Type serviceType)
        {
            var settings = new QmlBuildNotificationSettings(new TraceExtensionLog());
            return Task.FromResult<object?>(new QmlBuildNotificationSettingsService(this, settings));
        }

        private Task<object?> CreateVsUserNotificationProviderAsync(
            IAsyncServiceContainer container,
            CancellationToken cancellationToken,
            Type serviceType)
        {
            var log = new TraceExtensionLog();
            EnsureToolWindowNotifications(log);

            try {
                return Task.FromResult<object?>(
                    VsUserNotificationProviderAdapterFactory.Create(userNotificationProvider!, log));
            } catch (Exception ex) {
                log.Error("Qt Bridge: failed to create Visual Studio user notification provider "
                    + "adapter.", ex);
                return Task.FromResult<object?>(null);
            }
        }

        private void EnsureToolWindowNotifications(IExtensionLog log)
        {
            if (toolWindowNotificationSvc != null) {
                userNotificationProvider ??= new VsUserNotificationProvider(toolWindowNotificationSvc);
                return;
            }

            toolWindowNotificationSvc = new ToolWindowNotificationService(this, log);
            userNotificationProvider = new VsUserNotificationProvider(toolWindowNotificationSvc);
        }

        private async Task InitializeToolWindowNotificationCommandsAsync(CancellationToken ct)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var log = new TraceExtensionLog();
            if (await GetServiceAsync(typeof(SVsRegisterPriorityCommandTarget))
                is not IVsRegisterPriorityCommandTarget commandTargetRegistry) {
                log.Warning("Qt Bridge: Visual Studio priority command target registry was not "
                    + "available for tool window notification actions.");
                return;
            }

            ErrorHandler.ThrowOnFailure(commandTargetRegistry.RegisterPriorityCommandTarget(
                0,
                this,
                out toolWindowNotificationCommandTargetCookie));
            log.Verbose("Qt Bridge tool window notification action command registered: "
                + $"{ToolWindowNotificationCommands.CommandSet:D}:"
                + $"{ToolWindowNotificationCommands.InvokeActionCommandId}.");
        }

        private void UnregisterToolWindowNotificationCommandTarget()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (toolWindowNotificationCommandTargetCookie == 0)
                return;

            var log = new TraceExtensionLog();
            if (GetService(typeof(SVsRegisterPriorityCommandTarget))
                is IVsRegisterPriorityCommandTarget commandTargetRegistry) {
                ErrorHandler.ThrowOnFailure(commandTargetRegistry.UnregisterPriorityCommandTarget(
                    toolWindowNotificationCommandTargetCookie));
            } else {
                log.Warning("Qt Bridge: Visual Studio priority command target registry was not "
                    + "available while unregistering tool window notification actions.");
            }
            toolWindowNotificationCommandTargetCookie = 0;
        }

        int IOleCommandTarget.QueryStatus(
            ref Guid pguidCmdGroup,
            uint cCmds,
            OLECMD[] prgCmds,
            IntPtr pCmdText)
        {
            if (!IsToolWindowNotificationCommand(pguidCmdGroup, prgCmds, cCmds))
                return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;

            prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
            return VSConstants.S_OK;
        }

        int IOleCommandTarget.Exec(
            ref Guid pguidCmdGroup,
            uint nCmdId,
            uint nCmdexecopt,
            IntPtr pvaIn,
            IntPtr pvaOut)
        {
            if (pguidCmdGroup != ToolWindowNotificationCommands.CommandSet
                || nCmdId != ToolWindowNotificationCommands.InvokeActionCommandId) {
                return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            var commandParameter = pvaIn == IntPtr.Zero
                ? null
                : Marshal.GetObjectForNativeVariant(pvaIn)?.ToString();
            ExecuteToolWindowNotificationCommand(commandParameter);
            return VSConstants.S_OK;
        }

        private static bool IsToolWindowNotificationCommand(
            Guid commandGroup,
            IList<OLECMD> commands,
            uint commandCount)
        {
            return commandGroup == ToolWindowNotificationCommands.CommandSet
                && commandCount > 0
                && commands.Count > 0
                && commands[0].cmdID == ToolWindowNotificationCommands.InvokeActionCommandId;
        }

        private void ExecuteToolWindowNotificationCommand(string? commandParameter)
        {
            var log = new TraceExtensionLog();
            log.Info("Qt Bridge tool window notification action command invoked"
                + (commandParameter == null ? " without a parameter." : "."));
            _ = JoinableTaskFactory.RunAsync(async delegate
            {
                if (toolWindowNotificationSvc == null)
                    return;

                await toolWindowNotificationSvc.ExecuteCommandActionAsync(
                    commandParameter,
                    DisposalToken);
            });
        }
#endif

        private void OnVsThemeChanged(ThemeChangedEventArgs e)
        {
            _ = JoinableTaskFactory.RunAsync(async delegate
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
                Commands.WhatsNew.RefreshOpenWhatsNewPageForThemeChange();
            });
        }
    }
}
