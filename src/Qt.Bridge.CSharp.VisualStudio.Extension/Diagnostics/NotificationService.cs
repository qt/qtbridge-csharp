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

        public async Task ShowInfoAsync(string key, string message, CancellationToken ct) =>
            await ShowInfoAsync(key, message, [], ct);

        public async Task ShowInfoAsync(
            string key,
            string message,
            IReadOnlyCollection<NotificationAction> actions,
            CancellationToken ct)
        {
            log.Info($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(message, isError: false, actions, ct);
        }

        public async Task ShowWarningAsync(string key, string message, CancellationToken ct) =>
            await ShowWarningAsync(key, message, [], ct);

        public async Task ShowWarningAsync(
            string key,
            string message,
            IReadOnlyCollection<NotificationAction> actions,
            CancellationToken ct)
        {
            log.Warning($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(message, isError: false, [], ct);
        }

        public async Task ShowErrorAsync(string key, string message, CancellationToken ct) =>
            await ShowErrorAsync(key, message, [], ct);

        public async Task ShowErrorAsync(
            string key,
            string message,
            IReadOnlyCollection<NotificationAction> actions,
            CancellationToken ct)
        {
            log.Error($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(message, isError: true, [], ct);
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

        private async Task ShowInfoBarSafeAsync(
            string message,
            bool isError,
            IReadOnlyCollection<NotificationAction> actions,
            CancellationToken ct)
        {
            try {
                await ShowInfoBarAsync(message, isError, actions, ct);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                log.Warning($"Qt Bridge: failed to show InfoBar notification: {ex.Message}");
            }
        }

        private async Task ShowInfoBarAsync(
            string message,
            bool isError,
            IReadOnlyCollection<NotificationAction> actions,
            CancellationToken ct)
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
            var actionItems = actions.Select(action => new InfoBarButton(action.Text, action));
            var model = new InfoBarModel(message, actionItems, image, isCloseButtonVisible: true);

            var uiElement = factory.CreateInfoBar(model);
            if (uiElement != null) {
                if (actions.Count > 0) {
                    var events = new InfoBarEvents(log);
                    uiElement.Advise(events, out var cookie);
                    events.Initialize(cookie);
                }
                infoBarHost.AddInfoBar(uiElement);
            }
        }

        private sealed class InfoBarEvents(IExtensionLog log) : IVsInfoBarUIEvents
        {
            private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));
            private uint cookie;

            public void Initialize(uint eventCookie)
            {
                cookie = eventCookie;
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUiElement)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (cookie == 0)
                    return;
                infoBarUiElement.Unadvise(cookie);
                cookie = 0;
            }

            public void OnActionItemClicked(
                IVsInfoBarUIElement infoBarUiElement,
                IVsInfoBarActionItem actionItem)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (actionItem.ActionContext is not NotificationAction action)
                    return;

                _ = ExecuteActionAsync(action, infoBarUiElement, CancellationToken.None);
            }

            private async Task ExecuteActionAsync(
                NotificationAction action,
                IVsInfoBarUIElement infoBarUiElement,
                CancellationToken ct)
            {
                try {
                    await action.ExecuteAsync(ct);
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                    infoBarUiElement.Close();
                } catch (Exception ex) {
                    log.Warning($"Qt Bridge: notification action '{action.Text}' failed: "
                        + $"{ex.Message}");
                }
            }
        }
    }
}
