// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Notifications
{
    [ComVisible(true)]
    [Guid(ProviderGuidString)]
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class VsUserNotificationProvider
    {
        public const string ProviderGuidString = "A7A4D9F1-5F2E-4E4C-9A15-3B0A9D7E6C21";

        private readonly ToolWindowNotificationService notifications;

        internal VsUserNotificationProvider(ToolWindowNotificationService service) =>
            notifications = service ?? throw new ArgumentNullException(nameof(service));

        public Guid Id => notifications.ProviderId;

        public Task RefreshAsync(uint lcid) => notifications.RefreshAsync(lcid);

        public Task<bool> OnNotificationClickedAsync(Guid notificationId)
            => notifications.OnNotificationClickedAsync(notificationId);

        public Task<bool> OnNotificationDismissedAsync(Guid notificationId)
            => notifications.OnNotificationDismissedAsync(notificationId);

        public Task<bool> OnNotificationExpiredAsync(Guid notificationId)
            => notifications.OnNotificationExpiredAsync(notificationId);
    }

    internal static class VsUserNotificationProviderAdapterFactory
    {
        private const string UserNotificationsAssemblyName =
            "Microsoft.Internal.VisualStudio.UserNotifications";

        private const string ProviderInterfaceName =
            UserNotificationsAssemblyName + ".Internal.IVsUserNotificationProvider";

        private const string NotificationInterfaceName =
            UserNotificationsAssemblyName + ".Internal.IVsUserNotification";

        private static readonly Lazy<Type?> AdapterType = new(CreateAdapterType);

        public static object Create(VsUserNotificationProvider bridge, IExtensionLog log)
        {
            var type = AdapterType.Value;
            if (type == null) {
                log.Warning("Qt Bridge: Visual Studio user notification provider adapter could "
                    + "not be created because the required runtime interfaces were not found.");
                throw new InvalidOperationException(
                    "Required Visual Studio user notification runtime interfaces were not "
                    + "found.");
            }

            if (Activator.CreateInstance(type, bridge) is { } adapterType)
                return adapterType;

            throw new InvalidOperationException(
                "Visual Studio user notification provider adapter could not be created.");
        }

        private static Type? CreateAdapterType()
        {
            var providerInterface = GetRuntimeType(ProviderInterfaceName);
            var notificationInterface = GetRuntimeType(NotificationInterfaceName);
            if (providerInterface == null || notificationInterface == null)
                return null;

            var assemblyName = new AssemblyName("QtBridgeVsUserNotificationProviderAdapter");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType("Qt.Bridge.CSharp.VisualStudio.Extension."
                + "Diagnostics.RuntimeVsUserNotificationProviderAdapter",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(providerInterface);

            var bridgeField = typeBuilder.DefineField(
                "bridge",
                typeof(VsUserNotificationProvider),
                FieldAttributes.Private | FieldAttributes.InitOnly);

            DefineConstructor(typeBuilder, bridgeField);
            DefineIdGetter(typeBuilder, providerInterface, bridgeField);
            DefineNotificationMethod(
                typeBuilder,
                providerInterface,
                notificationInterface,
                bridgeField,
                "OnClickAsync",
                nameof(VsUserNotificationProvider.OnNotificationClickedAsync));
            DefineNotificationMethod(
                typeBuilder,
                providerInterface,
                notificationInterface,
                bridgeField,
                "OnDismissAsync",
                nameof(VsUserNotificationProvider.OnNotificationDismissedAsync));
            DefineNotificationMethod(
                typeBuilder,
                providerInterface,
                notificationInterface,
                bridgeField,
                "OnExpireAsync",
                nameof(VsUserNotificationProvider.OnNotificationExpiredAsync));
            DefineRefreshMethod(typeBuilder, providerInterface, bridgeField);

            return typeBuilder.CreateType();
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
            return type ?? Type.GetType(name + UserNotificationsAssemblyName, throwOnError: false);
        }

        private static void DefineConstructor(TypeBuilder typeBuilder, FieldInfo bridgeField)
        {
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                [typeof(VsUserNotificationProvider)]);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, bridgeField);
            il.Emit(OpCodes.Ret);
        }

        private static void DefineIdGetter(
            TypeBuilder typeBuilder,
            Type providerInterface,
            FieldInfo bridgeField)
        {
            var interfaceMethod = providerInterface.GetProperty("ID")?.GetMethod
                ?? throw new MissingMethodException(providerInterface.FullName, "get_ID");
            var method = typeBuilder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
                    | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                typeof(Guid),
                Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, bridgeField);
            il.Emit(OpCodes.Callvirt,
                typeof(VsUserNotificationProvider).GetProperty(nameof(
                    VsUserNotificationProvider.Id))!.GetMethod!);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(method, interfaceMethod);
        }

        private static void DefineNotificationMethod(
            TypeBuilder typeBuilder,
            Type providerInterface,
            Type notificationInterface,
            FieldInfo bridgeField,
            string interfaceMethodName,
            string bridgeMethodName)
        {
            var interfaceMethod = providerInterface.GetMethod(interfaceMethodName)
                ?? throw new MissingMethodException(
                    providerInterface.FullName,
                    interfaceMethodName);
            var identifierGetter = notificationInterface.GetProperty("Identifier")?.GetMethod
                ?? throw new MissingMethodException(notificationInterface.FullName, "get_Identifier");
            var bridgeMethod = typeof(VsUserNotificationProvider).GetMethod(bridgeMethodName)
                ?? throw new MissingMethodException(
                    typeof(VsUserNotificationProvider).FullName,
                    bridgeMethodName);

            var method = typeBuilder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
                    | MethodAttributes.HideBySig,
                typeof(Task<bool>),
                [notificationInterface]);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, bridgeField);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, identifierGetter);
            il.Emit(OpCodes.Callvirt, bridgeMethod);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(method, interfaceMethod);
        }

        private static void DefineRefreshMethod(
            TypeBuilder typeBuilder,
            Type providerInterface,
            FieldInfo bridgeField)
        {
            var interfaceMethod = providerInterface.GetMethod("RefreshAsync")
                ?? throw new MissingMethodException(providerInterface.FullName, "RefreshAsync");
            var bridgeMethod = typeof(VsUserNotificationProvider).GetMethod(nameof(
                VsUserNotificationProvider.RefreshAsync))
                ?? throw new MissingMethodException(
                    typeof(VsUserNotificationProvider).FullName,
                    nameof(VsUserNotificationProvider.RefreshAsync));

            var method = typeBuilder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
                    | MethodAttributes.HideBySig,
                typeof(Task),
                [typeof(uint)]);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, bridgeField);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, bridgeMethod);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(method, interfaceMethod);
        }
    }

    [Guid("153FA24E-5B64-4447-964E-FF57B2491A43")]
    public sealed class SVsUserNotificationServiceInternalInterop;
}
