// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Api
{
    public static class SafeArgumentMethods
    {
        public static string EchoUpperFirst(string value) => value;
        public static string EchoLowerFirst(string value) => value;
        public static string EchoValidation(string value) => value;

        public static string Upper(string value)
        {
            if (value == "invalid")
                throw new FormatException("Invalid argument");
            return value?.ToUpperInvariant();
        }

        public static string Lower(string value)
        {
            if (value == "invalid")
                throw new FormatException("Invalid argument");
            return value?.ToLowerInvariant();
        }

        public static int WrongReturn(string value) => 0;
        public static string WrongParameter(int value) => value.ToString();
    }

    internal class UpperCaseMarshaler : CustomMarshaler<string>
    {
        static UpperCaseMarshaler()
        {
            SafeArgumentConversion = new SafeArgumentConversion(
                new Parameter(typeof(StringMarshaler).AssemblyQualifiedName,
                    (ulong)UnmanagedType.CustomMarshaler),
                typeof(SafeArgumentMethods).GetMethod(nameof(SafeArgumentMethods.Upper)));
        }

        private static UpperCaseMarshaler Instance { get; } = new();
        public static ICustomMarshaler GetInstance(string _) => Instance;
        public static new SafeArgumentConversion SafeArgumentConversion { get; }

        public override int NativeSize => IntPtr.Size;
        public override string MarshalIn(nint ptr)
        {
            return SafeArgumentMethods.Upper((string)StringMarshaler.GetInstance("")
                .MarshalNativeToManaged(ptr));
        }

        public override nint MarshalOut(string value) => Marshal.StringToHGlobalUni(value);
    }

    internal class LowerCaseMarshaler : UpperCaseMarshaler
    {
        static LowerCaseMarshaler()
        {
            SafeArgumentConversion = UpperCaseMarshaler.SafeArgumentConversion with
            {
                Method = typeof(SafeArgumentMethods).GetMethod(nameof(SafeArgumentMethods.Lower))
            };
        }

        private static LowerCaseMarshaler Instance { get; } = new();
        public static new ICustomMarshaler GetInstance(string _) => Instance;
        public static new SafeArgumentConversion SafeArgumentConversion { get; }

        public override string MarshalIn(nint ptr)
        {
            return SafeArgumentMethods.Lower((string)StringMarshaler.GetInstance("")
                .MarshalNativeToManaged(ptr));
        }
    }

    internal class WrongReturnMarshaler : UpperCaseMarshaler
    {
        static WrongReturnMarshaler()
        {
            SafeArgumentConversion = UpperCaseMarshaler.SafeArgumentConversion with
            {
                Method = typeof(SafeArgumentMethods)
                    .GetMethod(nameof(SafeArgumentMethods.WrongReturn))
            };
        }

        public static new ICustomMarshaler GetInstance(string cookie) =>
            UpperCaseMarshaler.GetInstance(cookie);
        public static new SafeArgumentConversion SafeArgumentConversion { get; }
    }

    internal class WrongParameterMarshaler : UpperCaseMarshaler
    {
        static WrongParameterMarshaler()
        {
            SafeArgumentConversion = UpperCaseMarshaler.SafeArgumentConversion with
            {
                Method = typeof(SafeArgumentMethods)
                    .GetMethod(nameof(SafeArgumentMethods.WrongParameter))
            };
        }

        public static new ICustomMarshaler GetInstance(string cookie) =>
            UpperCaseMarshaler.GetInstance(cookie);
        public static new SafeArgumentConversion SafeArgumentConversion { get; }
    }

    [TestClass]
    public class Test_SafeArgumentConversion
    {
        private static Parameter MarshalerParam(Type type) =>
            new(type.AssemblyQualifiedName, (ulong)UnmanagedType.CustomMarshaler);
        private static Parameter StringParam => MarshalerParam(typeof(StringMarshaler));
        private static Parameter ObjectParam =>
            new(typeof(object).AssemblyQualifiedName, (ulong)Parameter.ObjectRef);

        private static Parameter SlotParam => new(typeof(nint));

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void SameTransport_UsesTheRequestedConversion(bool upperFirst)
        {
            var upper = MarshalerParam(typeof(UpperCaseMarshaler));
            var lower = MarshalerParam(typeof(LowerCaseMarshaler));
            var method = Adapter
                .ResolveStaticMethod(typeof(SafeArgumentMethods).AssemblyQualifiedName, upperFirst
                    ? nameof(SafeArgumentMethods.EchoUpperFirst)
                    : nameof(SafeArgumentMethods.EchoLowerFirst), 0, [StringParam, upper]);
            Parameter[] upperParams = [StringParam, ObjectParam, upper, SlotParam];
            Parameter[] lowerParams = [StringParam, ObjectParam, lower, SlotParam];
            var first = Adapter.ResolveSafeMethod(method, 0, upperFirst ? upperParams : lowerParams);
            var second = Adapter.ResolveSafeMethod(method, 0, upperFirst ? lowerParams : upperParams);
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(first,
                Adapter.ResolveSafeMethod(method, 0, upperFirst ? upperParams : lowerParams));

            var upperInvoke = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)
                (upperFirst ? first : second);
            var lowerInvoke = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)
                (upperFirst ? second : first);
            nint exception = 0;
            try {
                var result = upperInvoke(0, Marshal.StringToHGlobalUni("invalid"),
                    (nint)(&exception));
                Assert.AreEqual(nint.Zero, result);
                Assert.IsInstanceOfType<FormatException>(
                    Adapter.GetObjectRefFromPtr(exception).Target);
                Adapter.FreeObjectRef(exception);
                exception = 0;

                result = upperInvoke(0, Marshal.StringToHGlobalUni("Mixed"), (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual("MIXED",
                    StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
                result = lowerInvoke(0, Marshal.StringToHGlobalUni("Mixed"), (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual("mixed",
                    StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
                result = lowerInvoke(0, 0, (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual(nint.Zero, result);
            } finally {
                if (exception != 0)
                    Adapter.FreeObjectRef(exception);
            }
        }

        [TestMethod]
        [DataRow(typeof(WrongReturnMarshaler))]
        [DataRow(typeof(WrongParameterMarshaler))]
        public void InvalidConversion_IsRejectedBeforeGeneratingCode(Type marshaler)
        {
            var parameter = MarshalerParam(marshaler);
            var method = Adapter.ResolveStaticMethod(typeof(SafeArgumentMethods).AssemblyQualifiedName,
                nameof(SafeArgumentMethods.EchoValidation), 0, [StringParam, parameter]);
            Assert.ThrowsExactly<ArgumentException>(() => Adapter.ResolveSafeMethod(method, 0,
                [StringParam, ObjectParam, parameter, SlotParam]));
        }

        [TestMethod]
        public void Conversion_PreservesTheCallersParameterFlags()
        {
            var parameter = new Parameter(typeof(UpperCaseMarshaler).AssemblyQualifiedName,
                (ulong)UnmanagedType.CustomMarshaler | (1UL << 16));
            var conversion = parameter.GetSafeArgumentConversion();
            Assert.IsTrue(conversion.Transport.IsIn);
            Assert.AreEqual(typeof(StringMarshaler).AssemblyQualifiedName,
                conversion.Transport.TypeName);
            Assert.AreEqual(typeof(UpperCaseMarshaler).AssemblyQualifiedName, parameter.TypeName);
            Assert.IsNull(StringParam.GetSafeArgumentConversion());
            Assert.IsNull(ObjectParam.GetSafeArgumentConversion());
        }
    }
}
