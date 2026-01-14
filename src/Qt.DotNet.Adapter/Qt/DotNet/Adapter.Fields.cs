// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;

namespace Qt.DotNet
{
    public partial class Adapter
    {
        public static IntPtr ResolveStaticFieldGet(
            string typeName,
            string fieldName,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveStaticFieldGet(ResolveStaticFieldGet);
#endif
            return ResolveStaticFieldAccess(typeName, fieldName, false, parameters);
        }

        public static IntPtr ResolveStaticFieldSet(
            string typeName,
            string fieldName,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveStaticFieldSet(ResolveStaticFieldSet);
#endif
            return ResolveStaticFieldAccess(typeName, fieldName, true, parameters);
        }

        public static IntPtr ResolveInstanceFieldGet(
            IntPtr objRefPtr,
            string fieldName,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveInstanceFieldGet(ResolveInstanceFieldGet);
#endif
            return ResolveInstanceFieldAccess(objRefPtr, fieldName, false, parameters);

        }

        public static IntPtr ResolveInstanceFieldSet(
            IntPtr objRefPtr,
            string fieldName,
            int parameterCount,
            Parameter[] parameters)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.ResolveInstanceFieldSet(ResolveInstanceFieldSet);
#endif
            return ResolveInstanceFieldAccess(objRefPtr, fieldName, true, parameters);
        }

        private static IntPtr ResolveStaticFieldAccess(
            string typeName,
            string fieldName,
            bool isFieldSet,
            Parameter[] parameters)
        {
            var type = Type.GetType(typeName)
                ?? throw new ArgumentException($"Type '{typeName}' not found", nameof(typeName));
            return ResolveFieldAccess(type, null, fieldName, isFieldSet, parameters);
        }

        private static IntPtr ResolveInstanceFieldAccess(
            IntPtr objRefPtr, string fieldName, bool isFieldSet, Parameter[] parameters)
        {
            var objRef = GetObjectRefFromPtr(objRefPtr);
            if (objRef == null)
                throw new ArgumentException("Invalid object reference", nameof(objRefPtr));
            var obj = objRef.Target;
            var type = obj.GetType();

            return ResolveFieldAccess(type, obj, fieldName, isFieldSet, parameters);
        }

        private static IntPtr ResolveFieldAccess(
            Type type, object obj, string fieldName, bool isFieldSet, Parameter[] parameters)
        {
            var isStatic = (obj == null);
            object target = isStatic ? type : obj;
            var fieldFlags = BindingFlags.Public
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance);

            var fieldAccess = isFieldSet ? MemberAccess.FieldSet : MemberAccess.FieldGet;

            var field = type.GetField(fieldName, fieldFlags)
                ?? throw new ArgumentException($"Field not found [{fieldName}]", nameof(fieldName));

            bool sigOk =
                (isFieldSet, isStatic) switch
                {
                    (true, true) => parameters.Length == 2
                        && parameters[0].GetParameterType() == typeof(void)
                        && parameters[1].GetParameterType() == field.FieldType,
                    (true, false) => parameters.Length == 3
                        && parameters[0].GetParameterType() == typeof(void)
                        && parameters[1].GetParameterType() == typeof(object)
                        && parameters[2].GetParameterType() == field.FieldType,
                    (false, true) => parameters.Length == 1
                        && parameters[0].GetParameterType() == field.FieldType,
                    (false, false) => parameters.Length == 2
                        && parameters[0].GetParameterType() == field.FieldType
                        && parameters[1].GetParameterType() == typeof(object),
                };
            if (!sigOk)
                throw new ArgumentException($"Invalid signature", nameof(parameters));

            if (TryGetDelegate(target, field, fieldAccess, out var fieldMethod))
                return fieldMethod.FuncPtr;

            var fieldProxy = CodeGenerator.CreateProxyMethodForField(field, isFieldSet, parameters)
                ?? throw new InvalidOperationException("Failed to create proxy");

            var delegateType = CodeGenerator.CreateDelegateTypeForMethod(fieldProxy, parameters)
                ?? throw new InvalidOperationException("Failed to generate delegate type");

            var methodDelegate = Delegate.CreateDelegate(delegateType, fieldProxy, false)
                ?? throw new InvalidOperationException("Failed to create delegate");

            var methodHandle = GCHandle.Alloc(methodDelegate);
            var methodFuncPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);

            var delegateRef = new DelegateRef(methodHandle, methodFuncPtr);
            AddDelegateToCache(methodFuncPtr, target, field, fieldAccess, delegateRef);
            return methodFuncPtr;
        }
    }
}
