// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    public partial class Adapter
    {
        public static IntPtr ResolveStaticMethod(
            string typeName,
            string methodName,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveStaticMethod(ResolveStaticMethod);
#endif
            var type = Type.GetType(typeName)
                ?? throw new ArgumentException($"Type '{typeName}' not found", nameof(typeName));

            var sigTypes = parameters
                .Skip(1)
                .Select((x, i) => x.GetParameterType()
                    ?? throw new ArgumentException($"Type not found [{i}]", nameof(parameters)))
                .ToArray();

            var method = type.GetMethod(
                methodName, BindingFlags.Public | BindingFlags.Static, sigTypes)
                ?? throw new ArgumentException(
                    $"Method '{methodName}' not found", nameof(methodName));

            if (TryGetDelegateForMethod(type, method, out var objMethod))
                return objMethod.FuncPtr;

            var delegateType = CodeGenerator.CreateDelegateTypeForMethod(method, parameters)
                ?? throw new ArgumentException("Error getting method delegate", nameof(methodName));

            var methodDelegate = Delegate.CreateDelegate(delegateType, method, false)
                ?? throw new ArgumentException("Error getting method delegate", nameof(methodName));

            var methodHandle = GCHandle.Alloc(methodDelegate);
            var methodFuncPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);

            var delegateRef = new DelegateRef(methodHandle, methodFuncPtr);
            AddMethodDelegateToCache(methodFuncPtr, type, method, delegateRef);
            return methodFuncPtr;
        }

        public static IntPtr ResolveConstructor(
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveConstructor(ResolveConstructor);
#endif
            if (parameters == null || parameters.Length == 0)
                throw new ArgumentException("Null or empty param list", nameof(parameters));

            if (parameters[0].IsVoid)
                throw new ArgumentException("Constructor cannot return void", nameof(parameters));

            var type = parameters[0].GetParameterType()
                ?? throw new ArgumentException("Return type not found", nameof(parameters));

            var paramTypes = parameters
                .Skip(1)
                .Select((x, i) => x.GetParameterType()
                    ?? throw new ArgumentException($"Type not found [{i}]", nameof(parameters)))
                .ToArray();

            var ctor = type.GetConstructor(paramTypes)
                ?? throw new ArgumentException("Constructor not found", nameof(parameters));

            var ctorProxy = CodeGenerator.CreateProxyMethodForCtor(ctor, parameters)
                ?? throw new ArgumentException("Error getting ctor delegate", nameof(parameters));

            var delegateType = CodeGenerator.CreateDelegateTypeForMethod(ctorProxy, parameters)
                ?? throw new ArgumentException("Error getting ctor delegate", nameof(parameters));

            var methodDelegate = Delegate.CreateDelegate(delegateType, ctorProxy, false)
                ?? throw new ArgumentException("Error getting ctor delegate", nameof(parameters));

            var methodHandle = GCHandle.Alloc(methodDelegate);
            var methodFuncPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);

            var delegateRef = new DelegateRef(methodHandle, methodFuncPtr);
            AddCtorDelegateToCache(methodFuncPtr, type, ctor, delegateRef);
            return methodFuncPtr;
        }

        public static IntPtr ResolveInstanceMethod(
            IntPtr objRefPtr,
            string methodName,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveInstanceMethod(ResolveInstanceMethod);
#endif

            var objRef = GetObjectRefFromPtr(objRefPtr);
            if (objRef == null)
                throw new ArgumentException("Invalid object reference", nameof(objRefPtr));
            var obj = objRef.Target;
            var type = obj.GetType();
            var parameterTypes = parameters
                .Skip(1)
                .Select((x, i) => x.GetParameterType()
                    ?? throw new ArgumentException($"Type not found [{i}]", nameof(parameters)))
                .ToArray();

            var method = type.GetMethod(
                methodName, BindingFlags.Public | BindingFlags.Instance, parameterTypes)
                ?? throw new ArgumentException(
                    $"Method '{methodName}' not found", nameof(methodName));

            if (TryGetDelegateForMethod(obj, method, out var objMethod))
                return objMethod.FuncPtr;

            var delegateType = CodeGenerator.CreateDelegateTypeForMethod(method, parameters)
                ?? throw new ArgumentException("Error getting method delegate", nameof(methodName));

            var methodDelegate = Delegate.CreateDelegate(delegateType, obj, method, false)
                ?? throw new ArgumentException("Error getting method delegate", nameof(methodName));

            var methodHandle = GCHandle.Alloc(methodDelegate);
            var methodFuncPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);

            var delegateRef = new DelegateRef(methodHandle, methodFuncPtr);
            AddMethodDelegateToCache(methodFuncPtr, obj, method, delegateRef);
            return methodFuncPtr;
        }

        public static IntPtr ResolveSafeMethod(
            IntPtr funcPtr,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveSafeMethod(ResolveSafeMethod);
#endif
            if (TryGetSafeMethod(funcPtr, out var safeDelegateRef))
                return safeDelegateRef.FuncPtr;

            if (!DelegateRefs.TryGetValue(funcPtr, out var delegateInfo))
                throw new ArgumentException("Unknown function pointer", nameof(funcPtr));

            var delegateHandle = delegateInfo.Ref.Handle;
            var funcDelegate = delegateHandle.Target as Delegate;
            Debug.Assert(funcDelegate != null, nameof(funcDelegate) + " is null");

            var safeMethod = CodeGenerator.CreateSafeMethod(funcDelegate.Method);

            var delegateType = CodeGenerator.CreateDelegateTypeForMethod(safeMethod, parameters)
                ?? throw new Exception("Error getting safe method delegate");

            var methodDelegate = Delegate.CreateDelegate(delegateType, safeMethod)
                ?? throw new Exception("Error getting safe method delegate");

            var methodHandle = GCHandle.Alloc(methodDelegate);
            var methodFuncPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);

            var delegateRef = new DelegateRef(methodHandle, methodFuncPtr);
            AddSafeMethodToCache(funcPtr, delegateRef);
            return methodFuncPtr;
        }
    }
}
