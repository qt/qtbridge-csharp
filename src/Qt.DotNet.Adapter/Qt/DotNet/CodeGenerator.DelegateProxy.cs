// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    internal static partial class CodeGenerator
    {
        private static ConcurrentDictionary<Type, Type> DelegateProxyTypes { get; } = new();

        public static Type CreateDelegateProxyType(Type delegateType, Parameter[] parameters)
        {
            if (DelegateProxyTypes.TryGetValue(delegateType, out var proxyType))
                return proxyType;

            var invokeMethod = delegateType.GetMethod("Invoke")
                ?? throw new ArgumentException("Delegate Invoke method not found",
                    nameof(delegateType));
            if (parameters == null || parameters.Length == 0)
                throw new ArgumentException("Null or empty param list", nameof(parameters));

            var callbackParameters = new[]
            {
                parameters[0],
                new Parameter(typeof(IntPtr)),
                new Parameter(typeof(ulong)),
                new Parameter(typeof(IntPtr))
            }.Concat(parameters.Skip(1)).ToArray();
            var paramTypes = parameters
                .Skip(1)
                .Select((x, i) => x.GetParameterType()
                    ?? throw new ArgumentException($"Type not found [{i}]", nameof(parameters)))
                .ToArray();

#if TEST || DEBUG
            Debug.Assert(invokeMethod.ReturnType.IsAssignableTo(parameters[0].GetParameterType())
                || invokeMethod.ReturnType.IsAssignableFrom(parameters[0].GetParameterType()));
            Debug.Assert(invokeMethod.GetParameters().Zip(paramTypes)
                .All(x => x.First.ParameterType.IsAssignableTo(x.Second)
                    || x.First.ParameterType.IsAssignableFrom(x.Second)));
#endif

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
            var fieldError = typeGen.DefineField(
                "ErrorPtr", typeof(IntPtr), FieldAttributes.Public);
            var fieldContext = typeGen.DefineField(
                "ContextPtr", typeof(IntPtr), FieldAttributes.Public);
            var fieldCount = typeGen.DefineField(
                "Count", typeof(ulong), FieldAttributes.Public);

            var errorCallbackParameters = new[]
            {
                new Parameter(typeof(IntPtr)),
                new Parameter(typeof(IntPtr)),
                new Parameter(typeof(ulong))
            };
            var errorGen = typeGen.DefineNestedType(
                UniqueName(delegateType.Name, "ErrorCallback"),
                TypeAttributes.Sealed | TypeAttributes.NestedPublic,
                typeof(MulticastDelegate));
            var errorInvoke = InitDelegateType(errorGen, errorCallbackParameters);

            var monitorEnter = typeof(Monitor).GetMethod("Enter", [typeof(object)]);
            var monitorExit = typeof(Monitor).GetMethod("Exit", [typeof(object)]);
            var proxyCleanUp = typeof(InterfaceProxy).GetMethod(nameof(InterfaceProxy.CleanUp));
            var getDelegateForFunctionPointer = typeof(Marshal).GetMethod(
                nameof(Marshal.GetDelegateForFunctionPointer),
                [typeof(IntPtr), typeof(Type)]);
            var stringIsNullOrEmpty = typeof(string).GetMethod(
                nameof(string.IsNullOrEmpty), [typeof(string)]);
            var invalidOperationCtor = typeof(InvalidOperationException)
                .GetConstructor([typeof(string)]);
            var ptrToStringUni = typeof(Marshal).GetMethod(
                nameof(Marshal.PtrToStringUni), [typeof(IntPtr)]);

#if TEST || DEBUG
            Debug.Assert(monitorEnter != null && monitorExit != null);
            Debug.Assert(proxyCleanUp != null, nameof(proxyCleanUp) + " is null");
            Debug.Assert(getDelegateForFunctionPointer != null,
                nameof(getDelegateForFunctionPointer) + " is null");
            Debug.Assert(stringIsNullOrEmpty != null, nameof(stringIsNullOrEmpty) + " is null");
            Debug.Assert(invalidOperationCtor != null, nameof(invalidOperationCtor) + " is null");
            Debug.Assert(ptrToStringUni != null, nameof(ptrToStringUni) + " is null");
#endif

            var invokeGen = typeGen.DefineMethod("Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                invokeMethod.ReturnType, paramTypes);
            var code = invokeGen.GetILGenerator();

            var localOffset = 0;
            if (invokeMethod.ReturnType != typeof(void)) {
                code.DeclareLocal(invokeMethod.ReturnType);
                code.Emit(OpCodes.Ldloca_S, 0);
                code.Emit(OpCodes.Initobj, invokeMethod.ReturnType);
                localOffset = 1;
            }
            code.DeclareLocal(typeof(string));
            var errorLocal = localOffset;

            code.BeginExceptionBlock();

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCallback);
            code.Emit(OpCodes.Call, monitorEnter);

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCount);
            code.Emit(OpCodes.Ldc_I4_1);
            code.Emit(OpCodes.Conv_U8);
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

            var noErrorLabel = code.DefineLabel();
            var skipErrorQueryLabel = code.DefineLabel();

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldError);
            code.Emit(OpCodes.Brfalse_S, skipErrorQueryLabel);

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldError);
            code.Emit(OpCodes.Ldtoken, errorGen);
            code.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
            code.Emit(OpCodes.Call, getDelegateForFunctionPointer);
            code.Emit(OpCodes.Castclass, errorGen);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldContext);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCount);
            code.Emit(OpCodes.Callvirt, errorInvoke);
            code.Emit(OpCodes.Call, ptrToStringUni);
            code.Emit(OpCodes.Stloc_S, errorLocal);
            code.Emit(OpCodes.Br_S, noErrorLabel);

            code.MarkLabel(skipErrorQueryLabel);
            code.Emit(OpCodes.Ldnull);
            code.Emit(OpCodes.Stloc_S, errorLocal);

            code.MarkLabel(noErrorLabel);

            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCleanup);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldContext);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, fieldCount);
            code.Emit(OpCodes.Callvirt, proxyCleanUp);

            code.Emit(OpCodes.Ldloc_S, errorLocal);
            code.Emit(OpCodes.Call, stringIsNullOrEmpty);
            var doneLabel = code.DefineLabel();
            code.Emit(OpCodes.Brtrue_S, doneLabel);
            code.Emit(OpCodes.Ldloc_S, errorLocal);
            code.Emit(OpCodes.Newobj, invalidOperationCtor);
            code.Emit(OpCodes.Throw);
            code.MarkLabel(doneLabel);

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
                _ = errorGen.CreateType();
                proxyType = typeGen.CreateType();
            } catch (Exception exception) {
                throw new TypeAccessException("Error creating delegate proxy type", exception);
            }

            DelegateProxyTypes.TryAdd(delegateType, proxyType);
            return proxyType;
        }
    }
}
