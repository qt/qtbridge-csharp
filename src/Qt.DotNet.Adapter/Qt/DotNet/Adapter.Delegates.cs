// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    public partial class Adapter
    {
        /// <summary>
        /// Delegate types for Adapter public functions.
        /// </summary>
        public static class Delegates
        {
            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler,
                MarshalTypeRef = typeof(ObjectMarshaler))]
            public delegate Assembly LoadAssembly(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string assemblyName);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveStaticMethod(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string targetType,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string methodName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveConstructor(
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveInstanceMethod(
                [In] IntPtr objRefPtr,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string methodName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveStaticFieldGet(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string typeName,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string fieldName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveStaticFieldSet(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string typeName,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string fieldName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveInstanceFieldGet(
                [In] IntPtr objRefPtr,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string fieldName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveInstanceFieldSet(
                [In] IntPtr objRefPtr,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string fieldName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr ResolveSafeMethod(
                [In] IntPtr funcPtr,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
                [In] Parameter[] parameters);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void NativeEventHandler(
                [In] IntPtr context,
                [MarshalAs(UnmanagedType.LPWStr)]
                [In] string eventName,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(ObjectMarshaler))]
                [In] object sender,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(ObjectMarshaler))]
                [In] object args);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void AddEventHandler(
                [In] IntPtr objRefPtr,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string eventName,
                [In] IntPtr context,
                [In] NativeEventHandler eventHandler);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void RemoveEventHandler(
                [In] IntPtr objRefPtr,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string eventName,
                [In] IntPtr context);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void RemoveAllEventHandlersByContext(
                [In] IntPtr context);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr AddObjectRef(
                [In] IntPtr objRefPtr,
                [In] bool weakRef);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void FreeDelegateRef(
                [In] IntPtr delRefPtr);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void FreeObjectRef(
                [In] IntPtr objRefPtr);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void FreeTypeRef(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string typeName);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler,
                MarshalTypeRef = typeof(StringMarshaler))]
            public delegate string GetStableAssemblyQualifiedName([In] IntPtr typeRefPtr);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler,
                MarshalTypeRef = typeof(ObjectMarshaler))]
            public delegate InterfaceProxy AddInterfaceProxy(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string interfaceName,
                [In] IntPtr data,
                [In] IntPtr cleanUp);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.CustomMarshaler,
                MarshalTypeRef = typeof(ObjectMarshaler))]
            public delegate Delegate AddDelegateProxy(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string delegateTypeName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
                [In] Parameter[] parameters,
                [In] IntPtr data,
                [In] IntPtr deleteData,
                [In] IntPtr callbackPtr,
                [In] IntPtr cleanUpPtr,
                [In] IntPtr errorPtr,
                [In] IntPtr context);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void SetInterfaceMethod(
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(ObjectMarshaler))]
                [In] InterfaceProxy proxy,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string methodName,
                [In] int parameterCount,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
                [In] Parameter[] parameters,
                [In] IntPtr callbackPtr,
                [In] IntPtr cleanUpPtr,
                [In] IntPtr context);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate IntPtr GetObject(
                [In] IntPtr objRefPtr,
                [MarshalAs(UnmanagedType.CustomMarshaler,
                    MarshalTypeRef = typeof(StringMarshaler))]
                [In] string path);

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void Reset();

            [UnmanagedFunctionPointer(CallingConvention.Winapi)]
            public delegate void Stats(
                [Out] out int refCount,
                [Out] out int staticCount,
                [Out] out int eventCount);
        }
    }
}
