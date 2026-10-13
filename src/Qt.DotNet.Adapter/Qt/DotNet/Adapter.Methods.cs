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

            var method = type.FindMethod(
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

            if (TryGetDelegateForCtor(type, ctorProxy, out var ctorMethod))
                return ctorMethod.FuncPtr;

            var delegateType = CodeGenerator.CreateDelegateTypeForMethod(ctorProxy, parameters)
                ?? throw new ArgumentException("Error getting ctor delegate", nameof(parameters));

            var methodDelegate = Delegate.CreateDelegate(delegateType, ctorProxy, false)
                ?? throw new ArgumentException("Error getting ctor delegate", nameof(parameters));

            var methodHandle = GCHandle.Alloc(methodDelegate);
            var methodFuncPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);

            var delegateRef = new DelegateRef(methodHandle, methodFuncPtr);
            AddCtorDelegateToCache(methodFuncPtr, type, ctorProxy, delegateRef);
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

            var method = type.FindMethod(
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

    public static class MethodLookupExtensions
    {
        /// <summary>
        /// Checks if a given base method is hidden in the specified derived class.
        /// </summary>
        /// <param name="method">The base method to check.</param>
        /// <param name="derivedType">The derived type that might be hiding the method.</param>
        public static bool IsHiddenBy(this MethodInfo method, Type derivedType)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(derivedType);

            if (method.IsStatic)
                return false;

            if (!derivedType.IsSubclassOf(method.DeclaringType))
                return false;

            var candidate = derivedType.GetMethod(method.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                method.GetParameters().Select(p => p.ParameterType).ToArray());
            if (candidate == null || candidate.DeclaringType != derivedType)
                return false;
            if (candidate.IsVirtual && candidate.GetBaseDefinition() == method.GetBaseDefinition())
                return false;

            return true;
        }

        /// <summary>
        /// Extends the functionality of <see cref="Type.GetMethod(string, BindingFlags, Type[])"/>
        /// with the relaxed enumeration type binding rules of <see cref="BindsTo(Type, Type)"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> where the method is declared.</param>
        /// <param name="name">Name of the method to find.</param>
        /// <param name="flags">
        /// Corresponds to the <see cref="BindingFlags"/> parameter of
        /// <see cref="Type.GetMethod(string, BindingFlags, Type[])"/>
        /// </param>
        /// <param name="argTypes">
        /// Corresponds to the <see cref="Type"/> array parameter of
        /// <see cref="Type.GetMethod(string, BindingFlags, Type[])"/>
        /// </param>
        /// <returns>
        /// <see cref="MethodInfo"/> object that matches the specified name, flags and signature;
        /// <see langword="null"/> in case no matching method was found.
        /// </returns>
        public static MethodInfo FindMethod(
            this Type type, string name, BindingFlags flags, Type[] argTypes, bool strict = true)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            // Try strict param type binding using System.Type.GetMethod()
            if (strict && type.GetMethod(name, flags, argTypes) is { } method)
                return method;

            // Fallback to relaxed param type matching
            var candidates = type.GetMethods(flags)
                .Where(candidate => !candidate.IsHiddenBy(type)
                    && candidate.Name == name && candidate.BindsTo(argTypes));
            if (candidates.Count() != 1)
                return null;
            return candidates.Single();
        }

        /// <summary>
        /// Check method signature binding.
        /// </summary>
        /// <param name="method">Method to check.</param>
        /// <param name="argTypes">Actual parameter types.</param>
        /// <param name="returnType">Expected return type</param>
        /// <returns>
        /// <para>
        /// <see cref="bool">true</see> if the actual parameter types can be bound to the
        /// corresponding formal types;
        /// <see cref="bool">false</see> otherwise.
        /// </para>
        /// <para>
        /// If <paramref name="returnType"/> is specified, also checks if the method return type
        /// can be bound to the expected return type.
        /// </para>
        /// </returns>
        /// <remarks>
        /// Uses the relaxed type binding rules of <see cref="BindsTo(Type, Type)"/>.
        /// </remarks>
        public static bool BindsTo(this MethodInfo method, Type[] argTypes, Type returnType = null)
        {
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(argTypes);

            if (returnType?.BindsTo(method.ReturnType) is false)
                return false;

            var methodParams = method.GetParameters();
            if (methodParams.Length != argTypes.Length)
                return false;

            return methodParams
                .Select((param, idx) => (Formal: param.ParameterType, Actual: argTypes[idx]))
                .All(param => param.Formal.BindsTo(param.Actual));
        }

        /// <summary>
        /// Check parameter type binding.
        /// </summary>
        /// <param name="formal">Formal type, i.e. type of the parameter declaration</param>
        /// <param name="actual">Actual type, i.e. type passed for the parameter in a call</param>
        /// <returns>
        /// <see cref="bool">true</see> if the actual type is assignable to the formal type;
        /// <see cref="bool">false</see> otherwise.
        /// </returns>
        /// <remarks>
        /// For <see cref="System.Enum">enumeration types</see>, a relaxed binding is applied that
        /// checks compatibility with either the enumeration type or its underlying integer type.
        /// </remarks>
        public static bool BindsTo(this Type formal, Type actual)
        {
            ArgumentNullException.ThrowIfNull(formal);
            ArgumentNullException.ThrowIfNull(actual);

            return (formal, actual) switch
            {
                ({ IsEnum: false }, { IsEnum: false }) => actual.IsAssignableTo(formal),

                ({ IsEnum: true }, { IsEnum: false }) => actual.IsAssignableTo(formal)
                    || actual.IsAssignableTo(Enum.GetUnderlyingType(formal)),

                ({ IsEnum: false }, { IsEnum: true }) => actual.IsAssignableTo(formal)
                    || Enum.GetUnderlyingType(actual).IsAssignableTo(formal),

                ({ IsEnum: true }, { IsEnum: true }) => actual.IsAssignableTo(formal)
                    || Enum.GetUnderlyingType(actual).IsAssignableTo(Enum.GetUnderlyingType(formal))
            };
        }
    }
}
