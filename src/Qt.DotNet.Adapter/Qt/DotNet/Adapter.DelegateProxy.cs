// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    public partial class Adapter
    {
        public static Delegate AddDelegateProxy(
            string delegateTypeName,
            int parameterCount,
            Parameter[] parameters,
            IntPtr data,
            IntPtr deleteData,
            IntPtr callbackPtr,
            IntPtr cleanUpPtr,
            IntPtr context)
        {
#if DEBUG
            _ = new Delegates.AddDelegateProxy(AddDelegateProxy);
#endif
            var delegateType = Type.GetType(delegateTypeName)
                ?? throw new ArgumentException(
                    $"Delegate '{delegateTypeName}' not found", nameof(delegateTypeName));
            if (!delegateType.IsAssignableTo(typeof(Delegate)))
                throw new ArgumentException(
                    $"Type '{delegateTypeName}' is not a delegate", nameof(delegateTypeName));
            if (parameters == null || parameters.Length == 0)
                throw new ArgumentException("Null or empty param list", nameof(parameters));
            Debug.Assert(parameterCount == parameters.Length,
                $"parameterCount ({parameterCount}) != parameters.Length ({parameters.Length})");

            var proxyType = CodeGenerator.CreateDelegateProxyType(delegateType, parameters);
            var ctor = proxyType.GetConstructor(Array.Empty<Type>());

            Debug.Assert(ctor != null, nameof(ctor) + " is null");

            if (ctor.Invoke(null) is not InterfaceProxy proxy)
                throw new InvalidOperationException(
                    $"Error creating delegate proxy for {delegateTypeName}");

            proxy.DataPtr = data;
            proxy.CleanUpData = deleteData;

            var fieldCallback = proxyType.GetField("NativeCallback");
            var fieldCleanup = proxyType.GetField("CleanUpPtr");
            var fieldContext = proxyType.GetField("ContextPtr");

            Debug.Assert(fieldCallback != null, nameof(fieldCallback) + " is null");
            Debug.Assert(fieldCleanup != null, nameof(fieldCleanup) + " is null");
            Debug.Assert(fieldContext != null, nameof(fieldContext) + " is null");

            var callbackType = fieldCallback.FieldType;
            var callbackDelegate = Marshal.GetDelegateForFunctionPointer(callbackPtr, callbackType);
            fieldCallback.SetValue(proxy, callbackDelegate);
            fieldCleanup.SetValue(proxy, cleanUpPtr);
            fieldContext.SetValue(proxy, context);

            return Delegate.CreateDelegate(delegateType, proxy, "Invoke")
                ?? throw new InvalidOperationException(
                    $"Error binding delegate proxy for {delegateTypeName}");
        }
    }
}
