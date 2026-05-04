// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Qt.DotNet
{
    internal static partial class CodeGenerator
    {
        private static ConcurrentDictionary<Type, Type> DelegateProxyTypes { get; } = new();

        public static Type CreateDelegateProxyType(Type delegateType)
        {
            if (DelegateProxyTypes.TryGetValue(delegateType, out var proxyType))
                return proxyType;

            var invokeMethod = delegateType.GetMethod("Invoke")
                ?? throw new ArgumentException("Delegate Invoke method not found",
                    nameof(delegateType));

            var parameterInfos = invokeMethod.GetParameters();
            var paramTypes = parameterInfos
                .Select(x => x.ParameterType)
                .ToArray();
            var callbackParamTypes = paramTypes
                .Prepend(typeof(IntPtr))
                .Prepend(typeof(ulong))
                .Prepend(typeof(IntPtr))
                .ToArray();
            var callbackParameters = callbackParamTypes
                .Prepend(invokeMethod.ReturnType)
                .Select(t => new Parameter(t))
                .ToArray();

            var typeGen = ModuleGen.DefineType(UniqueName("DelegateProxy", delegateType.Name),
                TypeAttributes.Public, typeof(InterfaceProxy));

            var callbackGen = typeGen.DefineNestedType(
                UniqueName(delegateType.Name, "NativeCallback"),
                TypeAttributes.Sealed | TypeAttributes.NestedPublic,
                typeof(MulticastDelegate));
            var callbackInvoke = InitDelegateType(callbackGen, callbackParameters);

            var fieldCallback = typeGen.DefineField(
                "NativeCallback", callbackGen, FieldAttributes.Public);
            var fieldCleanup = typeGen.DefineField(
                "CleanUpPtr", typeof(IntPtr), FieldAttributes.Public);
            var fieldContext = typeGen.DefineField(
                "ContextPtr", typeof(IntPtr), FieldAttributes.Public);
            var fieldCount = typeGen.DefineField(
                "Count", typeof(ulong), FieldAttributes.Public);

            var monitorEnter = typeof(Monitor).GetMethod("Enter", new[] { typeof(object) });
            var monitorExit = typeof(Monitor).GetMethod("Exit", new[] { typeof(object) });
            var proxyCleanUp = typeof(InterfaceProxy).GetMethod(nameof(InterfaceProxy.CleanUp));

#if TEST || DEBUG
            Debug.Assert(monitorEnter != null && monitorExit != null);
            Debug.Assert(proxyCleanUp != null, nameof(proxyCleanUp) + " is null");
#endif

            var invokeGen = typeGen.DefineMethod("Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                invokeMethod.ReturnType, paramTypes);
            var code = invokeGen.GetILGenerator();

            if (invokeMethod.ReturnType != typeof(void)) {
                code.DeclareLocal(invokeMethod.ReturnType);
                code.Emit(OpCodes.Ldloca_S, 0);
                code.Emit(OpCodes.Initobj, invokeMethod.ReturnType);
            }

            code.BeginExceptionBlock();

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCallback);
            code.Emit(OpCodes.Call, monitorEnter);

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCount);
            code.Emit(OpCodes.Ldc_I4_1);
            code.Emit(OpCodes.Conv_I8);
            code.Emit(OpCodes.Add);
            code.Emit(OpCodes.Stfld, fieldCount);

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCallback);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldContext);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCount);
            code.Emit(OpCodes.Ldarg_0);
            const string nameOf = nameof(InterfaceProxy.DataPtr);
            code.Emit(OpCodes.Ldfld, typeof(InterfaceProxy).GetField(nameOf));

            for (var paramIdx = 0; paramIdx < paramTypes.Length; ++paramIdx) {
                switch (paramIdx) {
                case 0:
                    code.Emit(OpCodes.Ldarg_1);
                    break;
                case 1:
                    code.Emit(OpCodes.Ldarg_2);
                    break;
                case 2:
                    code.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    code.Emit(OpCodes.Ldarg_S, paramIdx + 1);
                    break;
                }
            }

            code.Emit(OpCodes.Callvirt, callbackInvoke);

            if (invokeMethod.ReturnType != typeof(void))
                code.Emit(OpCodes.Stloc_0);

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCleanup);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldContext);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCount);
            code.Emit(OpCodes.Callvirt, proxyCleanUp);

            code.BeginFinallyBlock();

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCallback);
            code.Emit(OpCodes.Call, monitorExit);

            code.EndExceptionBlock();

            if (invokeMethod.ReturnType != typeof(void))
                code.Emit(OpCodes.Ldloc_0);
            code.Emit(OpCodes.Ret);

            try {
                _ = callbackGen.CreateType();
                proxyType = typeGen.CreateType();
            } catch (Exception exception) {
                throw new TypeAccessException("Error creating delegate proxy type", exception);
            }

            DelegateProxyTypes.TryAdd(delegateType, proxyType);
            return proxyType;
        }
    }
}
