// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics
{
    /// <summary>
    /// Shows rate-limited, non-modal Visual Studio InfoBar notifications for user-facing failures.
    /// </summary>
    internal sealed class NotificationService(IExtensionLog log) : INotificationService
    {
        private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));
        private readonly HashSet<string> notifiedKeys = [];
        private readonly object notifiedKeysLock = new();

        public async Task ShowInfoAsync(string key, string message, CancellationToken ct)
        {
            log.Info($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(message, isError: false, ct);
        }

        public async Task ShowWarningAsync(string key, string message, CancellationToken ct)
        {
            log.Warning($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(message, isError: false, ct);
        }

        public async Task ShowErrorAsync(string key, string message, CancellationToken ct)
        {
            log.Error($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(message, isError: true, ct);
        }

        public void ClearRateLimit(string key)
        {
            lock (notifiedKeysLock) {
                notifiedKeys.Remove(key);
            }
        }

        private bool TryMarkNotified(string key)
        {
            lock (notifiedKeysLock) {
                return notifiedKeys.Add(key);
            }
        }

        private async Task ShowInfoBarSafeAsync(string message, bool isError, CancellationToken ct)
        {
            try {
                await ShowInfoBarAsync(message, isError, ct);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                log.Warning($"Qt Bridge: failed to show InfoBar notification: {ex.Message}");
            }
        }

        private static async Task ShowInfoBarAsync(string msg, bool isError, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            if (Package.GetGlobalService(typeof(SVsShell)) is not IVsShell vsShell)
                return;

            vsShell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var host);
            if (host is not IVsInfoBarHost infoBarHost)
                return;

            var tmp = Package.GetGlobalService(typeof(SVsInfoBarUIFactory));
            if (tmp is not IVsInfoBarUIFactory factory)
                return;

            var image = isError ? KnownMonikers.StatusError : KnownMonikers.StatusWarning;
            var textSpans = new InfoBarTextSpan[] { new(msg) };
            var model = new InfoBarModel(textSpans, image, isCloseButtonVisible: true);
            var uiElement = factory.CreateInfoBar(model);
            if (uiElement != null)
                infoBarHost.AddInfoBar(uiElement);
        }
    }
}
