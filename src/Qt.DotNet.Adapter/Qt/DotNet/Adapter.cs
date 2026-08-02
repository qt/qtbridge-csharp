// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using static Qt.DotNet.Adapter;

internal class StartupHook
{
    private static ConcurrentBag<DelegateRef> Callbacks { get; set; } = new();

    private static string PtrToNativeString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        // hostfxr callbacks provide UTF-16 strings on Windows and UTF-8 strings on Unix.
        // Using PtrToStringUni on Linux corrupts paths and type/method names.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Marshal.PtrToStringUni(ptr) ?? string.Empty;
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    public static void Initialize()
    {
        if (AppContext.GetData("QT_DOTNET_RESOLVE_FN") is not string refFnPtrHex)
            return;
        var refFnPtrInt = Convert.ToInt64(refFnPtrHex, 16);
        var refFnPtr = new IntPtr(refFnPtrInt);
        var fnPtr = Marshal.ReadIntPtr(refFnPtr);
        if (fnPtr != IntPtr.Zero)
            return;
        var delegateType = typeof(ResolveFnDelegate);
        var method = typeof(StartupHook).GetMethod("ResolveFn");
        var methodDelegate = Delegate.CreateDelegate(delegateType, method, false);
        var methodHandle = GCHandle.Alloc(methodDelegate);
        fnPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);
        Callbacks.Add(new DelegateRef(methodHandle, fnPtr));
        Marshal.WriteIntPtr(refFnPtr, fnPtr);
    }

    public static int ResolveFn(
        IntPtr assemblyPathNative,
        IntPtr typeNameNative,
        IntPtr methodNameNative,
        IntPtr delegateTypeNative,
        IntPtr reserved,
        IntPtr functionHandle)
    {
        string assemblyPath = PtrToNativeString(assemblyPathNative);
        string typeName = PtrToNativeString(typeNameNative);
        string methodName = PtrToNativeString(methodNameNative);
        string delegateTypeName = PtrToNativeString(delegateTypeNative);
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var type = Type.GetType(typeName);
        var delegateType = string.IsNullOrEmpty(delegateTypeName) ? typeof(EntryPointDelegate) : Type.GetType(delegateTypeName);
        var invoke = delegateType.GetMethod("Invoke");
        var paramTypes = invoke.GetParameters().Select(x => x.ParameterType).ToArray();
        var method = type.GetMethod(methodName, paramTypes);
        var methodDelegate = Delegate.CreateDelegate(delegateType, method, false);
        var methodHandle = GCHandle.Alloc(methodDelegate);
        var fnPtr = Marshal.GetFunctionPointerForDelegate(methodDelegate);
        Callbacks.Add(new DelegateRef(methodHandle, fnPtr));
        Marshal.WriteIntPtr(functionHandle, fnPtr);
        return 0;
    }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate int ResolveFnDelegate(
        IntPtr assemblyPathNative,
        IntPtr typeNameNative,
        IntPtr methodNameNative,
        IntPtr delegateTypeNative,
        IntPtr reserved,
        IntPtr functionHandle);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate int EntryPointDelegate(IntPtr arg, int argLength);
}

namespace Qt.DotNet
{
    /// <summary>
    /// Provides access to managed types, allowing native code to obtain references to, and call
    /// constructors, static methods and instance methods.
    /// </summary>
    public partial class Adapter
    {
        internal static Thread MainThread { get; private set; }

        public static void SetMainThread()
        {
            MainThread = Thread.CurrentThread;
        }

        public static bool IsMainThread => MainThread == Thread.CurrentThread;

        /// <summary>
        /// Loads a .NET assembly into memory
        /// </summary>
        /// <param name="assemblyName">Name of the assembly or path to the assembly DLL.</param>
        /// <returns>'true' if load was successful; 'false' otherwise</returns>
        public static bool LoadAssembly(string assemblyName)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.LoadAssembly(LoadAssembly);
#endif
            try {
                Assembly.Load(assemblyName);
                return true;
            } catch (Exception) {
            }

            if (File.Exists(Path.GetFullPath(assemblyName))) {
                try {
                    Assembly.LoadFile(Path.GetFullPath(assemblyName));
                    return true;
                } catch (Exception) {
                }
            }

            if (File.Exists(Path.GetFullPath($"{assemblyName}.dll"))) {
                try {
                    Assembly.LoadFile(Path.GetFullPath($"{assemblyName}.dll"));
                    return true;
                } catch (Exception) {
                }
            }
            return false;
        }

        /// <summary>
        /// Reset adapter cache
        /// </summary>
        public static void Reset()
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.Reset(Reset);
#endif
            ObjectRefs.Clear();
            DelegateRefs.Clear();
            DelegatesByMember.Clear();
            Events.Clear();
        }

        internal class DelegateRef
        {
            public GCHandle Handle { get; }
            public bool IsValid => Handle.IsAllocated;
            public Delegate Target => Handle.Target as Delegate;
            public IntPtr FuncPtr { get; }
            public DelegateRef(GCHandle handle, IntPtr funcPtr)
            {
                Handle = handle;
                FuncPtr = funcPtr;
            }
        }

        internal class ObjectRef
        {
            public GCHandle Handle { get; }
            public object Target => Handle.Target;
            public bool IsValid => Handle.IsAllocated && Handle.Target != null;
            public ObjectRef(GCHandle handle)
            {
                Handle = handle;
            }
        }

        private enum MemberAccess
        {
            Constructor = MemberTypes.Constructor,
            Method = MemberTypes.Method,
            FieldGet = MemberTypes.Field,
            FieldSet = -MemberTypes.Field
        }

        private static ConcurrentDictionary
            <IntPtr, ObjectRef> ObjectRefs
        { get; } = new();

        private static ConcurrentDictionary
            <IntPtr, (object Target, MemberInfo Member, MemberAccess Access, DelegateRef Ref)>
            DelegateRefs
        { get; } = new();

        private static ConcurrentDictionary
            <(object Target, MemberInfo Member, MemberAccess Access), DelegateRef> DelegatesByMember
        { get; } = new();

        private static ConcurrentDictionary
            <(ObjectRef Source, string Name, IntPtr Context), EventRelay> Events
        { get; } = new();

        private static ConcurrentDictionary
            <nint, (DelegateRef Ref, nint UnsafeFuncPtr)> SafeMethodsBySafePtr
        { get; } = new();

        private static ConcurrentDictionary<nint, DelegateRef> SafeMethodsByUnsafePtr
        { get; } = new();

        private static void AddDelegateToCache(
            IntPtr ptr, object obj, MemberInfo member, MemberAccess access, DelegateRef delegateRef)
        {
            DelegateRefs.TryAdd(ptr, (obj, member, access, delegateRef));
            DelegatesByMember.TryAdd((obj, member, access), delegateRef);
        }

        private static bool TryGetDelegate(
            object obj, MemberInfo member, MemberAccess access, out DelegateRef delegateRef)
        {
            return DelegatesByMember.TryGetValue((obj, member, access), out delegateRef);
        }

        private static void AddMethodDelegateToCache(
            IntPtr ptr, object obj, MethodInfo method, DelegateRef delegateRef)
        {
            AddDelegateToCache(ptr, obj, method, MemberAccess.Method, delegateRef);
        }

        private static bool TryGetDelegateForMethod(
            object obj, MethodInfo method, out DelegateRef delegateRef)
        {
            return TryGetDelegate(obj, method, MemberAccess.Method, out delegateRef);
        }

        private static void AddCtorDelegateToCache(
            IntPtr ptr, object obj, ConstructorInfo ctor, DelegateRef delegateRef)
        {
            AddDelegateToCache(ptr, obj, ctor, MemberAccess.Constructor, delegateRef);
        }

        private static bool TryGetDelegateForCtor(
            object obj, ConstructorInfo ctor, out DelegateRef delegateRef)
        {
            return TryGetDelegate(obj, ctor, MemberAccess.Constructor, out delegateRef);
        }

        private static void AddSafeMethodToCache(nint ptr, DelegateRef delegateRef)
        {
            SafeMethodsByUnsafePtr.TryAdd(ptr, delegateRef);
            SafeMethodsBySafePtr.TryAdd(delegateRef.FuncPtr, (delegateRef, ptr));
        }

        private static bool TryGetSafeMethod(nint ptr, out DelegateRef delegateRef)
        {
            return SafeMethodsByUnsafePtr.TryGetValue(ptr, out delegateRef);
        }
    }
}
