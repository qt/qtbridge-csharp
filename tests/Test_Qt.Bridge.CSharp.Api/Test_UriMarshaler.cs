// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Api
{
    public static class SafeUriMethods
    {
        public static string Echo(Uri value) => value?.OriginalString;
        public static string EchoObject(object value) => ((Uri)value)?.OriginalString;
        public static string EchoObjectFirst(Uri value) => value?.OriginalString;
        public static string EchoTextFirst(Uri value) => value?.OriginalString;
        public static string Pair(Uri left, int marker, Uri right)
            => $"{left?.OriginalString}|{marker}|{right?.OriginalString}";
        public static Uri ReturnUri() => new Uri("images/icon.png", UriKind.Relative);
    }

    [TestClass]
    public class Test_UriMarshaler
    {
        private static Parameter StringParam => new(typeof(StringMarshaler).AssemblyQualifiedName,
            (ulong)UnmanagedType.CustomMarshaler);
        private static Parameter UriParam => new(typeof(UriMarshaler).AssemblyQualifiedName,
            (ulong)UnmanagedType.CustomMarshaler);
        private static Parameter ObjectParam => new(typeof(object).AssemblyQualifiedName,
            (ulong)Parameter.ObjectRef);
        private static Parameter SlotParam => new(typeof(nint));

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("images/icon.png")]
        [DataRow("../images/icon.png?size=2#icon")]
        [DataRow("https://example.org/images/icon.png")]
        public void RoundTripsNullEmptyRelativeAndAbsoluteUris(string text)
        {
            var marshaler = UriMarshaler.GetInstance("");
            var uri = (Uri)marshaler.MarshalNativeToManaged(Marshal.StringToHGlobalUni(text));
            Assert.AreEqual(text, uri?.OriginalString);
            var result = marshaler.MarshalManagedToNative(uri);
            try {
                Assert.AreEqual(text, Marshal.PtrToStringUni(result));
            } finally {
                Marshal.FreeHGlobal(result);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void SafeMethod_ReportsInvalidUriAndRecovers(bool objectParameter)
        {
            var method = Adapter.ResolveStaticMethod(typeof(SafeUriMethods).AssemblyQualifiedName,
                objectParameter ? nameof(SafeUriMethods.EchoObject) : nameof(SafeUriMethods.Echo),
                0, [StringParam, UriParam]);
            Parameter[] parameters = [StringParam, ObjectParam, UriParam, SlotParam];
            var safe = Adapter.ResolveSafeMethod(method, 0, parameters);
            Assert.AreEqual(typeof(UriMarshaler).AssemblyQualifiedName, parameters[2].TypeName);
            Assert.AreEqual(safe, Adapter.ResolveSafeMethod(method, 0, parameters));
            var invoke = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)safe;
            nint exception = 0;
            try {
                var result = invoke(0, Marshal.StringToHGlobalUni("http://["), (nint)(&exception));
                Assert.AreEqual(nint.Zero, result);
                Assert.IsInstanceOfType<UriFormatException>(
                    Adapter.GetObjectRefFromPtr(exception).Target);
                Adapter.FreeObjectRef(exception);
                exception = 0;
                foreach (var text in new[] { null, "", "images/icon.png", "https://example.org/" }) {
                    result = invoke(0, Marshal.StringToHGlobalUni(text), (nint)(&exception));
                    Assert.AreEqual(nint.Zero, exception);
                    Assert.AreEqual(text,
                        StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
                }
            } finally {
                if (exception != 0)
                    Adapter.FreeObjectRef(exception);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void SafeMethod_ConvertsOnlyTheRequestedUriArguments(bool textFirst)
        {
            var number = new Parameter(typeof(int));
            var uriReference = new Parameter(typeof(Uri).AssemblyQualifiedName,
                (ulong)Parameter.ObjectRef);
            var method = Adapter.ResolveStaticMethod(typeof(SafeUriMethods).AssemblyQualifiedName,
                nameof(SafeUriMethods.Pair), 0, [StringParam, UriParam, number, UriParam]);
            var safe = Adapter.ResolveSafeMethod(method, 0,
                [StringParam, ObjectParam, textFirst ? UriParam : uriReference, number,
                 textFirst ? uriReference : UriParam, SlotParam]);
            var invoke = (delegate* unmanaged[Stdcall]<nint, nint, int, nint, nint, nint>)safe;
            var uriRef = Adapter.GetRefPtrToObject(new Uri("object.png", UriKind.Relative));
            nint exception = 0;
            try {
                var invalid = Marshal.StringToHGlobalUni("http://[");
                var result = invoke(0, textFirst ? invalid : uriRef, 42,
                    textFirst ? uriRef : invalid, (nint)(&exception));
                Assert.AreEqual(nint.Zero, result);
                Assert.IsInstanceOfType<UriFormatException>(
                    Adapter.GetObjectRefFromPtr(exception).Target);
                Adapter.FreeObjectRef(exception);
                exception = 0;
                var text = Marshal.StringToHGlobalUni("text.png");
                result = invoke(0, textFirst ? text : uriRef, 42,
                    textFirst ? uriRef : text, (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual(textFirst ? "text.png|42|object.png" : "object.png|42|text.png",
                    StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
            } finally {
                if (exception != 0)
                    Adapter.FreeObjectRef(exception);
                Adapter.FreeObjectRef(uriRef);
            }
        }

        [TestMethod]
        public unsafe void SafeMethod_PreservesUriReturnMarshaling()
        {
            var method = Adapter.ResolveStaticMethod(typeof(SafeUriMethods).AssemblyQualifiedName,
                nameof(SafeUriMethods.ReturnUri), 0, [UriParam]);
            var safe = Adapter.ResolveSafeMethod(method, 0, [UriParam, ObjectParam, SlotParam]);
            var invoke = (delegate* unmanaged[Stdcall]<nint, nint, nint>)safe;
            nint exception = 0;
            try {
                var result = invoke(0, (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual("images/icon.png",
                    StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
            } finally {
                if (exception != 0)
                    Adapter.FreeObjectRef(exception);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void SafeMethod_SeparatesUriTextAndObjectReferences(bool objectFirst)
        {
            var method = Adapter.ResolveStaticMethod(typeof(SafeUriMethods).AssemblyQualifiedName,
                objectFirst ? nameof(SafeUriMethods.EchoObjectFirst)
                    : nameof(SafeUriMethods.EchoTextFirst),
                0, [StringParam, UriParam]);
            var uriReference = new Parameter(typeof(Uri).AssemblyQualifiedName,
                (ulong)Parameter.ObjectRef);
            Parameter[] objectParams = [StringParam, ObjectParam, uriReference, SlotParam];
            Parameter[] uriParams = [StringParam, ObjectParam, UriParam, SlotParam];
            var first = Adapter.ResolveSafeMethod(method, 0, objectFirst ? objectParams : uriParams);
            var second = Adapter.ResolveSafeMethod(method, 0, objectFirst ? uriParams : objectParams);
            Assert.AreNotEqual(first, second);
            var uriInvoke = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)
                (objectFirst ? second : first);
            var objectInvoke = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)
                (objectFirst ? first : second);
            var uriRef = Adapter.GetRefPtrToObject(new Uri("images/icon.png", UriKind.Relative));
            nint exception = 0;
            try {
                var result = objectInvoke(0, uriRef, (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual("images/icon.png",
                    StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
                result = uriInvoke(0, Marshal.StringToHGlobalUni("images/icon.png"),
                    (nint)(&exception));
                Assert.AreEqual(nint.Zero, exception);
                Assert.AreEqual("images/icon.png",
                    StringMarshaler.GetInstance("").MarshalNativeToManaged(result));
            } finally {
                if (exception != 0)
                    Adapter.FreeObjectRef(exception);
                Adapter.FreeObjectRef(uriRef);
            }
        }
    }
}
