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
        private readonly Dictionary<string, IVsInfoBarUIElement> activeInfoBars = [];
        private readonly object activeInfoBarsLock = new();

        public async Task ShowInfoAsync(string key, string message, CancellationToken ct) =>
            await ShowInfoAsync(key, message, [], ct);

        public async Task ShowInfoAsync(
            string key,
            string message,
            IEnumerable<NotificationAction> actions,
            CancellationToken ct)
        {
            log.Info($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(key, message, isError: false, actions, ct);
        }

        public async Task ShowWarningAsync(string key, string message, CancellationToken ct) =>
            await ShowWarningAsync(key, message, [], ct);

        public async Task ShowWarningAsync(
            string key,
            string message,
            IEnumerable<NotificationAction> actions,
            CancellationToken ct)
        {
            log.Warning($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(key, message, isError: false, actions, ct);
        }

        public async Task ShowErrorAsync(string key, string message, CancellationToken ct) =>
            await ShowErrorAsync(key, message, [], ct);

        public async Task ShowErrorAsync(
            string key,
            string message,
            IEnumerable<NotificationAction> actions,
            CancellationToken ct)
        {
            log.Error($"Notification[{key}]: {message}");
            if (TryMarkNotified(key))
                await ShowInfoBarSafeAsync(key, message, isError: true, actions, ct);
        }

        public async Task DismissAsync(string key, CancellationToken ct)
        {
            IVsInfoBarUIElement infoBar;
            lock (activeInfoBarsLock) {
                if (!activeInfoBars.TryGetValue(key, out infoBar))
                    return;
                activeInfoBars.Remove(key);
            }

            try {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                infoBar.Close();
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                log.Warning($"Qt Bridge: failed to dismiss InfoBar notification '{key}': "
                    + $"{ex.Message}");
            }
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
            string key,
            string message,
            bool isError,
            IEnumerable<NotificationAction> actions,
            CancellationToken ct)
        {
            try {
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
                    var events = new InfoBarEvents(log, key, RemoveActiveInfoBar);
                    uiElement.Advise(events, out var cookie);
                    events.Initialize(cookie);
                    lock (activeInfoBarsLock) {
                        activeInfoBars[key] = uiElement;
                    }
                    infoBarHost.AddInfoBar(uiElement);
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                log.Warning($"Qt Bridge: failed to show InfoBar notification: {ex.Message}");
            }
        }

        private void RemoveActiveInfoBar(string key, IVsInfoBarUIElement infoBarUiElement)
        {
            lock (activeInfoBarsLock) {
                if (activeInfoBars.TryGetValue(key, out var current)
                    && ReferenceEquals(current, infoBarUiElement)) {
                    activeInfoBars.Remove(key);
                }
            }
        }

        private sealed class InfoBarEvents(
            IExtensionLog log,
            string key,
            Action<string, IVsInfoBarUIElement> onClosed) : IVsInfoBarUIEvents
        {
            private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));
            private readonly string key = key ?? throw new ArgumentNullException(nameof(key));
            private readonly Action<string, IVsInfoBarUIElement> onClosed = onClosed
                ?? throw new ArgumentNullException(nameof(onClosed));
            private uint cookie;

            public void Initialize(uint eventCookie)
            {
                cookie = eventCookie;
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUiElement)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                onClosed(key, infoBarUiElement);
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
