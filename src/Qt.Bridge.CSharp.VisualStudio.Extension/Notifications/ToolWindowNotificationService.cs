// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Notifications
{
    /// <inheritdoc cref="IToolWindowNotificationService"/>
    internal sealed partial class ToolWindowNotificationService(AsyncPackage pkg, IExtensionLog log)
        : IToolWindowNotificationService
    {
        private readonly AsyncPackage package = pkg ?? throw new ArgumentNullException(nameof(pkg));
        private readonly IExtensionLog log = log ?? throw new ArgumentNullException(nameof(log));

        private readonly Dictionary<Guid, ToolWindowNotificationHandlers> transientHandlers = [];
        private readonly Dictionary<string, NotificationAction> commandActions =
            new(StringComparer.Ordinal);

        private readonly object syncLock = new();

        internal Guid ProviderId => new(VsUserNotificationProvider.ProviderGuidString);

        public async Task<Guid?> ShowAsync(
            string title,
            string description,
            IReadOnlyList<NotificationAction>? actions,
            CancellationToken ct)
        {
            var notificationManager = await GetNotificationManagerAsync(ct);
            if (notificationManager == null)
                return null;

            ValidateDurableActions(actions);
            var notificationId = Guid.NewGuid();
            try {
                var notification = TryAddNotification(
                    notificationManager,
                    notificationId,
                    title,
                    description,
                    actions);

                log.Info("Qt Bridge tool window notification posted: "
                    + $"{GetNotificationIdentifier(notification) ?? notificationId.ToString("D")}");
                return notificationId;
            } catch (Exception ex) {
                log.Warning("Qt Bridge: failed to post Visual Studio tool window notification: "
                    + ex.Message);
                return null;
            }
        }

        public async Task<Guid?> ShowTransientAsync(
            string title,
            string description,
            ToolWindowNotificationHandlers? handlers,
            CancellationToken ct)
        {
            var notificationManager = await GetNotificationManagerAsync(ct);
            if (notificationManager == null)
                return null;

            var notificationId = Guid.NewGuid();
            if (handlers != null) {
                lock (syncLock) {
                    transientHandlers[notificationId] = handlers;
                }
            }

            try {
                var notification = AddTransientNotification(
                    notificationManager,
                    notificationId,
                    title,
                    description);

                log.Info("Qt Bridge transient tool window notification posted: "
                    + $"{GetNotificationIdentifier(notification) ?? notificationId.ToString("D")}");
                return notificationId;
            } catch (Exception ex) {
                RemoveTransientHandlers(notificationId);
                log.Warning("Qt Bridge: failed to post transient Visual Studio tool window "
                    + $"notification: {ex.Message}");
                return null;
            }
        }

        private object TryAddNotification(
            object notificationManager,
            Guid notificationId,
            string title,
            string description,
            IReadOnlyList<NotificationAction>? actions)
        {
            if (VsCommandNotificationApi.TryCreate() is not { } api) {
                throw new InvalidOperationException("Visual Studio command-backed tool window "
                    + "notification creation is not available.");
            }

            return api.CreateAndAdd(
                notificationManager,
                ProviderId,
                notificationId,
                "Qt Bridge for C#",
                title,
                description,
                actions);
        }

        private object AddTransientNotification(
            object notificationManager,
            Guid notificationId,
            string title,
            string description)
        {
            var notification = InvokeInstanceMethod(
                notificationManager,
                "AddNotification",
                ProviderId,
                notificationId,
                title,
                description,
                notificationId.ToString("D"),
                true);
            if (notification == null) {
                throw new InvalidOperationException("Visual Studio did not accept the transient "
                    + "notification.");
            }
            return notification;
        }

        public void RegisterDurableAction(NotificationAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var actionId = action.ActionId;
            if (string.IsNullOrWhiteSpace(actionId)) {
                throw new ArgumentException(
                    "Durable tool window notification actions must provide a non-empty action id.",
                    nameof(action));
            }
            var durableActionId = actionId!;

            lock (syncLock) {
                if (commandActions.ContainsKey(durableActionId)) {
                    throw new InvalidOperationException("A tool window notification action with "
                        + $"id '{durableActionId}' is already registered.");
                }
                commandActions[durableActionId] = action;
            }
        }

        public Task DismissAsync(Guid notificationId, CancellationToken ct)
        {
            RemoveTransientHandlers(notificationId);
            return RemoveNotificationAsync(notificationId, ct);
        }

        internal async Task<bool> ExecuteCommandActionAsync(
            string? commandParameter,
            CancellationToken ct)
        {
            if (!TryParseCommandParameter(
                commandParameter,
                out var notificationId,
                out var actionId)) {
                log.Warning("Qt Bridge: invalid tool window notification command parameter.");
                return false;
            }

            log.Info("Qt Bridge tool window notification command invoked: "
                + $"{notificationId:D}, {actionId}");

            var action = GetNotificationCommandAction(actionId);
            if (action == null) {
                log.Warning("Qt Bridge: no command action registered for notification "
                    + $"{notificationId:D}, {actionId}");
                return false;
            }

            try {
                await action.ExecuteAsync(ct);
                return true;
            } catch (Exception ex) {
                log.Warning("Qt Bridge: notification command action failed: " + ex.Message);
                return false;
            }
        }

        internal Task RefreshAsync(uint lcid)
        {
            log.Verbose($"Qt Bridge tool window notification provider refresh requested: {lcid}");
            return Task.CompletedTask;
        }

        internal Task<bool> OnNotificationClickedAsync(Guid notificationId)
        {
            var handlers = GetTransientHandlers(notificationId);
            if (handlers?.OnClick != null)
                return ExecuteTransientClickAsync(notificationId, handlers.OnClick);

            log.Verbose($"Qt Bridge tool window notification clicked: {notificationId:D}");
            return Task.FromResult(true);
        }

        private async Task<bool> ExecuteTransientClickAsync(
            Guid notificationId,
            Func<CancellationToken, Task> callback)
        {
            log.Verbose($"Qt Bridge tool window notification clicked: {notificationId:D}");

            try {
                await callback(CancellationToken.None);
                RemoveTransientHandlers(notificationId);
                return true;
            } catch (Exception ex) {
                log.Warning("Qt Bridge: transient notification click action failed: " + ex.Message);
                return false;
            }
        }

        internal Task<bool> OnNotificationDismissedAsync(Guid notificationId)
        {
            log.Verbose($"Qt Bridge tool window notification dismissed: {notificationId:D}");
            return ExecuteTransientLifecycleAsync(
                notificationId,
                handlers => handlers.OnDismiss,
                "dismiss");
        }

        internal Task<bool> OnNotificationExpiredAsync(Guid notificationId)
        {
            log.Verbose($"Qt Bridge tool window notification expired: {notificationId:D}");
            return ExecuteTransientLifecycleAsync(
                notificationId,
                handlers => handlers.OnExpire,
                "expire");
        }

        private async Task<object?> GetNotificationManagerAsync(CancellationToken ct)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var vsSvs = Package.GetGlobalService(typeof(SVsUserNotificationServiceInternalInterop));
            if (vsSvs == null) {
                log.Warning("Qt Bridge: Visual Studio user notification service was not found.");
                return null;
            }

            try {
                var managerTask = InvokeInstanceMethod(vsSvs, "GetNotificationManagerAsync");
                if (managerTask is not Task task) {
                    log.Warning("Qt Bridge: GetNotificationManagerAsync did not return a Task.");
                    return null;
                }

                await task;
                var resultProperty = managerTask.GetType().GetProperty("Result");
                var notificationManager = resultProperty?.GetValue(managerTask);
                if (notificationManager != null)
                    return notificationManager;
                log.Warning("Qt Bridge: Visual Studio returned no user notification manager "
                    + "instance.");
                return null;
            } catch (Exception ex) {
                log.Warning("Qt Bridge: failed to get Visual Studio user notification manager: "
                    + ex.Message);
                return null;
            }
        }

        private static object? InvokeInstanceMethod(
            object instance,
            string methodName,
            params object?[] arguments)
        {
            return instance.GetType().InvokeMember(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
                binder: null,
                target: instance,
                args: arguments);
        }

        private static string? GetNotificationIdentifier(object? notification)
        {
            return notification?.GetType().GetProperty("Identifier")?.GetValue(notification)
                .ToString();
        }

        private NotificationAction? GetNotificationCommandAction(string actionId)
        {
            lock (syncLock) {
                commandActions.TryGetValue(actionId, out var registeredAction);
                return registeredAction;
            }
        }

        private void ValidateDurableActions(IReadOnlyList<NotificationAction>? actions)
        {
            if (actions is not { Count: > 0 })
                return;

            lock (syncLock) {
                foreach (var action in actions) {
                    if (string.IsNullOrWhiteSpace(action.ActionId)) {
                        throw new InvalidOperationException("Durable tool window notification "
                            + "actions must provide a non-empty action id.");
                    }
                    var actionId = action.ActionId!;

                    if (!commandActions.ContainsKey(actionId)) {
                        throw new InvalidOperationException("Durable tool window notification "
                            + "actions must be registered before showing the notification. "
                            + $"Missing action id: {actionId}");
                    }
                }
            }
        }

        private ToolWindowNotificationHandlers? GetTransientHandlers(Guid notificationId)
        {
            lock (syncLock) {
                transientHandlers.TryGetValue(notificationId, out var handlers);
                return handlers;
            }
        }

        private Task<bool> ExecuteTransientLifecycleAsync(
            Guid notificationId,
            Func<ToolWindowNotificationHandlers, Func<CancellationToken, Task>?> selector,
            string eventName)
        {
            var handlers = RemoveTransientHandlers(notificationId);
            var callback = handlers == null ? null : selector(handlers);
            if (callback == null)
                return Task.FromResult(true);

            return InvokeTransientLifecycleAsync(callback, eventName);
        }

        private async Task<bool> InvokeTransientLifecycleAsync(
            Func<CancellationToken, Task> callback,
            string eventName)
        {
            try {
                await callback(CancellationToken.None);
                return true;
            } catch (Exception ex) {
                log.Warning($"Qt Bridge: transient notification {eventName} callback failed: "
                    + ex.Message);
                return false;
            }
        }

        private ToolWindowNotificationHandlers? RemoveTransientHandlers(Guid notificationId)
        {
            if (notificationId == Guid.Empty)
                return null;

            lock (syncLock) {
                if (!transientHandlers.TryGetValue(notificationId, out var handlers))
                    return null;
                transientHandlers.Remove(notificationId);
                return handlers;
            }
        }

        private async Task RemoveNotificationAsync(Guid notificationId, CancellationToken ct)
        {
            if (notificationId == Guid.Empty)
                return;

            var notificationManager = await GetNotificationManagerAsync(ct);
            if (notificationManager == null)
                return;

            try {
                var obj = InvokeInstanceMethod(
                    notificationManager,
                    "GetNotifications",
                    ProviderId,
                    notificationId);
                if (obj is not IEnumerable notifications)
                    return;

                foreach (var notification in notifications)
                    InvokeInstanceMethod(notificationManager, "RemoveNotification", notification);
            } catch (Exception ex) {
                log.Warning("Qt Bridge: failed to remove tool window notification after "
                    + $"successful callback: {ex.Message}");
            }
        }

        private static string CreateCommandParameter(Guid notificationId, string actionId)
            => notificationId.ToString("D") + "|" + actionId;

        private static bool TryParseCommandParameter(
            string? value,
            out Guid notificationId,
            out string actionId)
        {
            notificationId = Guid.Empty;
            actionId = "";
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parameter = value!;
            var separator = parameter.IndexOf('|');
            if (separator <= 0 || separator == parameter.Length - 1)
                return false;

            if (!Guid.TryParse(parameter.Substring(0, separator), out notificationId))
                return false;

            actionId = parameter.Substring(separator + 1);
            return !string.IsNullOrWhiteSpace(actionId);
        }
    }
}
