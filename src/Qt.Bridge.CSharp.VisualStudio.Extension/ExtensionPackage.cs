// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
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
    [ProvideOptionPage(typeof(LoggingOptionsPage), "Qt Bridge for C#",
        "Logging", 0, 0, true, SupportsProfiles = true,
        IsInUnifiedSettings = true)]
#if WINDOWS_DESKTOP
    [ProvideOptionPage(typeof(NotificationOptionsPage), "Qt Bridge for C#",
        "Notifications", 0, 0, true, SupportsProfiles = false,
        IsInUnifiedSettings = true)]
    [ProvideService(typeof(IQmlBuildNotificationSettingsService), IsCacheable = true)]
#endif
    [ProvideSettingsManifest(PackageRelativeManifestFile = "QtBridge.registration.json")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string,
        PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    public sealed class ExtensionPackage : AsyncPackage
    {
        internal const string PackageGuidString = "D4E5F6A7-B8C9-4D2E-8F1A-3B5C7D9E1F2A";

        internal static LoggingOptionsPage? LoggingPage { get; private set; }

        public static ExtensionPackage? Instance { get; private set; }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            Instance = this;
#if WINDOWS_DESKTOP
            AddService(typeof(IQmlBuildNotificationSettingsService),
                CreateQmlBuildNotificationExternalSettingsServiceAsync,
                promote: true);
#endif
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            LoggingPage = (LoggingOptionsPage)GetDialogPage(typeof(LoggingOptionsPage));
            VSColorTheme.ThemeChanged += OnVsThemeChanged;

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
            VSColorTheme.ThemeChanged -= OnVsThemeChanged;
        }

        private async Task InitializeWhatsNewLifecycleAsync(CancellationToken cancellationToken)
        {
            var log = new TraceExtensionLog();
            var lifecycle = new WhatsNewLifecycleService(
                new SettingsStore(this),
                new ToastNotificationService(new NotificationService(log), log),
                log);

            await lifecycle.InitializeAsync(cancellationToken);
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
