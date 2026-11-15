// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    public interface IAdapterCustomMarshaler
    {
        static Type NativeType { get; }
    }

    /// <summary>
    /// Describes a transport and a public static conversion from its managed type to the
    /// marshaler's managed type. Safe methods run the conversion in their try block.
    /// </summary>
    public sealed record SafeArgumentConversion(Parameter Transport, MethodInfo Method);

    public abstract class CustomMarshaler<T> : IAdapterCustomMarshaler, ICustomMarshaler
        where T: class
    {
        public static Type NativeType => typeof(T);

        /// <summary>
        /// Optional conversion for safe calls. Derived marshalers can hide this property to
        /// describe a transport whose conversion to T must run inside the generated try block.
        /// </summary>
        public static SafeArgumentConversion SafeArgumentConversion => null;

        public abstract int NativeSize { get; }

        public abstract T MarshalIn(nint ptr);
        public abstract nint MarshalOut(T obj);
        public virtual void CleanUp(T obj) { }
        public virtual void CleanUp(nint ptr) { }

        public void CleanUpManagedData(object obj)
        {
            if (obj is not T objT)
                return;
            CleanUp(objT);
        }

        public void CleanUpNativeData(nint ptr)
        {
            CleanUp(ptr);
        }

        public int GetNativeDataSize()
        {
            return NativeSize;
        }

        public nint MarshalManagedToNative(object obj)
        {
            if (obj is not T objT)
                return nint.Zero;
            return MarshalOut(objT);
        }

        public object MarshalNativeToManaged(nint ptr)
        {
            return MarshalIn(ptr);
        }
    }

    /// <summary>
    /// Information describing a parameter or the return type of a function.
    /// Used to generate dynamic delegate types for methods requested through the Adapter.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Parameter
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TypeName = string.Empty;
        public readonly ulong ParamInfo = 0;

        // Format of parameter info field
        // 63......................31...........15............0
        // |------ ARRAYLENGTH -----|--- FLAGS --|--- TYPE ---|

        const int TYPE_OFFSET = 0;
        const int TYPE_SIZE = 16;

        const int FLAGS_OFFSET = TYPE_OFFSET + TYPE_SIZE;
        const int FLAGS_SIZE = 16;
        const int FLAGS_IN_BIT = 0;
        const int FLAGS_OUT_BIT = 1;
        const int FLAGS_ARRAY_BIT = 2;
        const int FLAGS_FIXEDLENGTH_BIT = 3;
        const int FLAGS_WEAKREF_BIT = 4;

        const int ARRAYLENGTH_OFFSET = FLAGS_OFFSET + FLAGS_SIZE;
        const int ARRAYLENGTH_SIZE = 32;

        private static ulong MASK(ulong value, int size) => value & ((((ulong)1) << size) - 1);
        private static bool FLAG(ulong value, int offset) => ((value >> offset) & 1) == 1;

        internal SafeArgumentConversion GetSafeArgumentConversion()
        {
            if (MarshalAs != UnmanagedType.CustomMarshaler)
                return null;
            var property = Type.GetType(TypeName)?.GetProperty(nameof(SafeArgumentConversion),
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (property?.GetValue(null) is not SafeArgumentConversion conversion)
                return null;

            // Keep the caller's direction and array flags when replacing the transport.
            var transport = new Parameter(conversion.Transport.TypeName,
                conversion.Transport.ParamInfo | (ParamInfo & ~MASK(ulong.MaxValue, TYPE_SIZE)));
            var method = conversion.Method;
            if (method == null
                || !method.IsStatic
                || !method.IsPublic
                || method.DeclaringType is not { IsVisible: true }
                || method.ContainsGenericParameters
                || method.GetParameters() is not { Length: 1 } arguments
                || arguments[0].ParameterType != transport.GetParameterType()
                || method.ReturnType != GetParameterType()) {
                throw new ArgumentException("Invalid safe argument conversion", nameof(TypeName));
            }
            return conversion with { Transport = transport };
        }

        public Type GetParameterType()
        {
            if (IsVoid)
                return typeof(void);
            if (MarshalAs == UnmanagedType.CustomMarshaler) {
                if (string.IsNullOrEmpty(TypeName))
                    return null;
                if (Type.GetType(TypeName) is not {} customMarshalerType)
                    return null;
                if (!customMarshalerType.IsAssignableTo(typeof(ICustomMarshaler)))
                    return null;

                if (customMarshalerType.IsAssignableTo(typeof(IAdapterCustomMarshaler))) {
                    var nativeType = customMarshalerType.GetProperty("NativeType",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (nativeType != null && nativeType.GetValue(null) is Type customType)
                        return customType;
                }
                return typeof(object);
            }
            if (!string.IsNullOrEmpty(TypeName))
                return Type.GetType(TypeName);
            string typeName = $"{nameof(System)}."
                + MarshalAs switch
                {
                    UnmanagedType.Bool => nameof(Boolean),
                    UnmanagedType.I1 => nameof(SByte),
                    UnmanagedType.U1 => nameof(Byte),
                    UnmanagedType.I2 => nameof(Int16),
                    UnmanagedType.U2 => nameof(UInt16),
                    UnmanagedType.I4 => nameof(Int32),
                    UnmanagedType.U4 => nameof(UInt32),
                    UnmanagedType.I8 => nameof(Int64),
                    UnmanagedType.U8 => nameof(UInt64),
                    UnmanagedType.R4 => nameof(Single),
                    UnmanagedType.R8 => nameof(Double),
                    UnmanagedType.SysInt => nameof(IntPtr),
                    UnmanagedType.SysUInt => nameof(UIntPtr),
                    UnmanagedType.FunctionPtr => nameof(Delegate),
                    UnmanagedType.LPStr => nameof(String),
                    UnmanagedType.LPWStr => nameof(String),
                    UnmanagedType.LPTStr => nameof(String),
                    UnmanagedType.ByValTStr => nameof(String),
                    _ => nameof(Object)
                }
                + (IsArray ? "[]" : "");

            var type = Type.GetType(typeName);
            if (type != null)
                TypeName = typeName;
            return type;
        }

        public Parameter(Type type)
        {
            TypeName = type.AssemblyQualifiedName;
            ParamInfo = 0;
        }

        public Parameter(string typeName, ulong paramInfo = 0)
        {
            TypeName = typeName;
            ParamInfo = paramInfo;
        }

        public Parameter(UnmanagedType type)
        {
            ParamInfo = MASK((ulong)type, TYPE_SIZE) << TYPE_OFFSET;
        }

        public static UnmanagedType ObjectRef => (UnmanagedType)MASK(ulong.MaxValue, TYPE_SIZE);
        public UnmanagedType MarshalAs => (UnmanagedType)MASK(ParamInfo >> TYPE_OFFSET, TYPE_SIZE);
        public bool IsIn => FLAG(ParamInfo >> FLAGS_OFFSET, FLAGS_IN_BIT);
        public bool IsOut => FLAG(ParamInfo >> FLAGS_OFFSET, FLAGS_OUT_BIT);
        public bool IsArray => FLAG(ParamInfo >> FLAGS_OFFSET, FLAGS_ARRAY_BIT);
        public bool IsFixedLength => FLAG(ParamInfo >> FLAGS_OFFSET, FLAGS_FIXEDLENGTH_BIT);
        public bool IsWeakRef => FLAG(ParamInfo >> FLAGS_OFFSET, FLAGS_WEAKREF_BIT);
        public int ArrayLength => (int)MASK(ParamInfo >> ARRAYLENGTH_OFFSET, ARRAYLENGTH_SIZE);
        public bool IsVoid => string.IsNullOrEmpty(TypeName) && MarshalAs == 0;
        public static Parameter Void { get; } = new();
    }
}
