// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Notifications
{
    internal sealed partial class ToolWindowNotificationService
    {
        private sealed class VsCommandNotificationApi
        {
            private const string UserNotificationsAssemblyName =
                "Microsoft.Internal.VisualStudio.UserNotifications";

            private const string ManagerInternalInterfaceName =
                UserNotificationsAssemblyName + ".Internal.IVsUserNotificationManagerInternal";
            private const string ContextTypeName =
                UserNotificationsAssemblyName + ".UserNotificationContext";
            private const string CommandDescriptionListTypeName =
                UserNotificationsAssemblyName + ".UserNotificationCommandDescriptionList";
            private const string CommandDescriptionInterfaceName =
                UserNotificationsAssemblyName + ".Internal.IVsUserNotificationCommandDescription";
            private const string VsCommandDescriptionTypeName =
                UserNotificationsAssemblyName + ".VsCommandCommandDescription";
            private const string CategoryTypeName =
                UserNotificationsAssemblyName + ".UserNotificationCategory";
            private const string PriorityTypeName =
                UserNotificationsAssemblyName + ".UserNotificationPriority";

            private static readonly Lazy<VsCommandNotificationApi?> Instance = new(Create);

            private readonly Type managerInternalInterface;
            private readonly Type contextType;
            private readonly Type commandDescriptionInterface;
            private readonly Type vsCommandDescriptionType;
            private readonly MethodInfo createNewNotificationMethod;
            private readonly MethodInfo addNotificationInternalMethod;
            private readonly ConstructorInfo commandDescriptionListConstructor;
            private readonly object emptyCommands;
            private readonly object extensionsCategory;
            private readonly object normalPriority;

            private VsCommandNotificationApi(
                Type managerInternalInterface,
                Type contextType,
                Type commandDescriptionInterface,
                Type vsCommandDescriptionType,
                MethodInfo createNewNotificationMethod,
                MethodInfo addNotificationInternalMethod,
                ConstructorInfo commandDescriptionListConstructor,
                object emptyCommands,
                object extensionsCategory,
                object normalPriority)
            {
                this.managerInternalInterface = managerInternalInterface;
                this.contextType = contextType;
                this.commandDescriptionInterface = commandDescriptionInterface;
                this.vsCommandDescriptionType = vsCommandDescriptionType;
                this.createNewNotificationMethod = createNewNotificationMethod;
                this.addNotificationInternalMethod = addNotificationInternalMethod;
                this.commandDescriptionListConstructor = commandDescriptionListConstructor;
                this.emptyCommands = emptyCommands;
                this.extensionsCategory = extensionsCategory;
                this.normalPriority = normalPriority;
            }

            public static VsCommandNotificationApi? TryCreate() => Instance.Value;

            public object CreateAndAdd(
                object notificationManager,
                Guid providerId,
                Guid notificationId,
                string identifierName,
                string title,
                string description,
                IReadOnlyList<NotificationAction>? actions)
            {
                if (!managerInternalInterface.IsInstanceOfType(notificationManager)) {
                    throw new InvalidOperationException("Visual Studio notification manager does "
                        + "not implement the internal notification manager interface.");
                }

                var context = Activator.CreateInstance(
                    contextType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: [notificationId.ToString("D")],
                    culture: null);
                if (context == null) {
                    throw new InvalidOperationException("Visual Studio notification context "
                        + "could not be created.");
                }

                var notification = createNewNotificationMethod.Invoke(notificationManager, [
                    providerId,
                    notificationId,
                    notificationId,
                    identifierName,
                    title,
                    description,
                    extensionsCategory,
                    normalPriority,
                    IsIgnorable,
                    context,
                    CreateCommandDescriptions(notificationId, actions),
                    IsTransient,
                    IsSticky
                ]);
                if (notification == null) {
                    throw new InvalidOperationException("Visual Studio returned no notification "
                        + "instance.");
                }

                var added = addNotificationInternalMethod.Invoke(
                    notificationManager,
                    [notification]);
                if (added is not true) {
                    throw new InvalidOperationException("Visual Studio did not accept the "
                        + "notification.");
                }

                return notification;
            }

            private object CreateCommandDescriptions(
                Guid notificationId,
                IReadOnlyList<NotificationAction>? actions)
            {
                if (actions is not { Count: > 0 })
                    return emptyCommands;

                var commandDescriptions = Array
                    .CreateInstance(commandDescriptionInterface, actions.Count);
                for (var index = 0; index < actions.Count; ++index) {
                    var action = actions[index];
                    if (string.IsNullOrWhiteSpace(action.ActionId)) {
                        throw new InvalidOperationException("Visual Studio notification command "
                            + "action id must not be empty.");
                    }
                    var actionId = action.ActionId!;

                    var commandDescription = Activator.CreateInstance(
                        vsCommandDescriptionType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        args: [
                            action.Text,
                            ToolWindowNotificationCommands.CommandSet,
                            ToolWindowNotificationCommands.InvokeActionCommandId,
                            true,
                            CreateCommandParameter(notificationId, actionId)
                        ],
                        culture: null);
                    if (commandDescription == null) {
                        throw new InvalidOperationException("Visual Studio notification command "
                            + "description could not be created.");
                    }
                    commandDescriptions.SetValue(commandDescription, index);
                }

                return commandDescriptionListConstructor.Invoke([commandDescriptions]);
            }

            // Durable tool-window notifications are stored by Visual Studio, so this path always
            // creates non-transient notifications.
            private const bool IsTransient = false;

            // Keep normal VS notification lifetime behavior. Product behavior must be modeled
            // as explicit command actions instead of depending on expiration or close callbacks.
            private const bool IsSticky = false;
            private const bool IsIgnorable = false;

            private static VsCommandNotificationApi? Create()
            {
                var managerInternalInterface = GetRuntimeType(ManagerInternalInterfaceName);
                var contextType = GetRuntimeType(ContextTypeName);
                var commandDescriptionListType = GetRuntimeType(CommandDescriptionListTypeName);
                var commandDescriptionInterface = GetRuntimeType(CommandDescriptionInterfaceName);
                var vsCommandDescriptionType = GetRuntimeType(VsCommandDescriptionTypeName);
                var categoryType = GetRuntimeType(CategoryTypeName);
                var priorityType = GetRuntimeType(PriorityTypeName);
                if (managerInternalInterface == null
                    || contextType == null
                    || commandDescriptionListType == null
                    || commandDescriptionInterface == null
                    || vsCommandDescriptionType == null
                    || categoryType == null
                    || priorityType == null) {
                    return null;
                }

                var createNewNotificationMethod = managerInternalInterface
                    .GetMethod("CreateNewNotification");
                var addNotificationInternalMethod = managerInternalInterface
                    .GetMethod("AddNotificationInternal");
                var commandDescriptionListConstructor = commandDescriptionListType
                    .GetConstructor([commandDescriptionInterface.MakeArrayType()]);
                var emptyCommands = commandDescriptionListType
                    .GetProperty("Empty", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (createNewNotificationMethod == null
                    || addNotificationInternalMethod == null
                    || commandDescriptionListConstructor == null
                    || emptyCommands == null) {
                    return null;
                }

                var extensionsCategory = Enum.Parse(categoryType, "Extensions");
                var normalPriority = Enum.Parse(priorityType, "Normal");
                return new VsCommandNotificationApi(
                    managerInternalInterface,
                    contextType,
                    commandDescriptionInterface,
                    vsCommandDescriptionType,
                    createNewNotificationMethod,
                    addNotificationInternalMethod,
                    commandDescriptionListConstructor,
                    emptyCommands,
                    extensionsCategory,
                    normalPriority);
            }

            private static Type? GetRuntimeType(string name)
            {
                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(assembly => string.Equals(
                        assembly.GetName().Name,
                        UserNotificationsAssemblyName,
                        StringComparison.Ordinal))
                    ?.GetType(name, throwOnError: false);
                return type ?? Type.GetType(
                    name + ", " + UserNotificationsAssemblyName,
                    throwOnError: false);
            }
        }
    }
}
