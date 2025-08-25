/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Qt.DotNet
{
    public class InterfaceProxy
    {
        public delegate void CleanUpDelegate(IntPtr context, ulong count);

        public void CleanUp(IntPtr callback, IntPtr context, ulong count)
        {
            if (callback == IntPtr.Zero)
                return;
            var CleanUp = Marshal.GetDelegateForFunctionPointer<CleanUpDelegate>(callback);
            CleanUp?.Invoke(context, count);
        }

        public IntPtr DataPtr = IntPtr.Zero;
        public IntPtr Data => DataPtr;
        public IntPtr CleanUpData { get; set; } = IntPtr.Zero;

        public delegate void DeleteDelegate(IntPtr data);

        ~InterfaceProxy()
        {
            if (Data == IntPtr.Zero || CleanUpData == IntPtr.Zero)
                return;
            Marshal.GetDelegateForFunctionPointer<DeleteDelegate>(CleanUpData)?.Invoke(Data);
        }
    }

    public partial class Adapter
    {
        public static InterfaceProxy AddInterfaceProxy(string interfaceName,
            IntPtr data, IntPtr cleanUp)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.AddInterfaceProxy(AddInterfaceProxy);
#endif
            var interfaceType = Type.GetType(interfaceName)
                ?? throw new ArgumentException(
                    $"Interface '{interfaceName}' not found", nameof(interfaceName));
            var proxyType = CodeGenerator.CreateInterfaceProxyType(interfaceType);
            var ctor = proxyType.GetConstructor(Array.Empty<Type>());

            Debug.Assert(ctor != null, nameof(ctor) + " is null");

            if (ctor.Invoke(null) is not InterfaceProxy proxy)
                throw new InvalidOperationException($"Error creating proxy for {interfaceName}");

            proxy.DataPtr = data;
            proxy.CleanUpData = cleanUp;
            return proxy;
        }

        public static void SetInterfaceMethod(
            InterfaceProxy proxy,
            string methodName,
            int parameterCount,
            Parameter[] parameters,
            IntPtr callbackPtr,
            IntPtr cleanUpPtr,
            IntPtr context)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.SetInterfaceMethod(SetInterfaceMethod);
#endif
            var type = proxy.GetType();
            var parameterTypes = parameters
                .Skip(4) // (return, context, key, data)
                .Select((x, i) => x.GetParameterType()
                    ?? throw new ArgumentException($"Type not found [{i}]", nameof(parameters)))
                .ToArray();

            var method = type.GetMethod(methodName, parameterTypes)
                ?? throw new ArgumentException(
                    $"Method '{methodName}' not found", nameof(methodName));

            var delegateType = CodeGenerator.CreateDelegateType(method.Name, parameters);
            var delegateTypeInvoke = delegateType.GetMethod("Invoke");

            Debug.Assert(delegateTypeInvoke != null, nameof(delegateTypeInvoke) + " != null");

            var paramTypes = delegateTypeInvoke.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();

            var callbacks = proxy.GetType().GetFields()
                .Where(f => Regex.IsMatch(f.Name, $@"^{methodName}_Callback_\w{{11}}$"))
                .ToArray();
            if (!callbacks.Any())
                throw new ArgumentException("Method not found", methodName);

            FieldInfo prototype = null;
            foreach (var callback in callbacks) {
                bool sigOk = callback.FieldType.IsAssignableTo(typeof(Delegate));
                var callbackInvoke = callback.FieldType.GetMethod("Invoke");
                sigOk = sigOk && callbackInvoke is not null;
                if (!sigOk)
                    continue;
                var callbackParams = callbackInvoke.GetParameters();
                sigOk = sigOk && callbackParams is not null;
                sigOk = sigOk && callbackParams.Length == paramTypes.Length;
                sigOk = sigOk && callbackParams.Zip(paramTypes)
                    .All(x => x.First.ParameterType == x.Second);
                if (!sigOk)
                    continue;

                prototype = callback;
                break;
            }
            if (prototype is null)
                throw new ArgumentException("Signature mismatch", nameof(parameters));

            var fieldCallback = proxy.GetType().GetField($"Call_{prototype.Name}");
            var callbackDelegate = Marshal.GetDelegateForFunctionPointer(callbackPtr, delegateType);

            Debug.Assert(fieldCallback != null, nameof(fieldCallback) + " is null");

            fieldCallback.SetValue(proxy, callbackDelegate);

            var fieldCleanup = proxy.GetType().GetField($"CleanUp_{prototype.Name}");

            Debug.Assert(fieldCleanup != null, nameof(fieldCleanup) + " is null");

            fieldCleanup.SetValue(proxy, cleanUpPtr);

            var fieldContext = proxy.GetType().GetField($"Context_{prototype.Name}");

            Debug.Assert(fieldContext != null, nameof(fieldContext) + " is null");

            fieldContext.SetValue(proxy, context);
        }
    }
}
