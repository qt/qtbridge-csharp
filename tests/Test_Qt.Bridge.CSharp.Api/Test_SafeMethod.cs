// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrimesApp;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Api
{
    [TestClass]
    public class Test_SafeMethod
    {
        private Parameter[] FuncParams(params Type[] types)
        {
            return [.. types.Select(t => new Parameter(t))];
        }

        private Parameter[] SafeFuncParams(params Type[] types)
        {
            return [new(typeof(object)), new(typeof(object)),
                .. types.Select(t => new Parameter(t)).Skip(1)];
        }

        private static Parameter Param<T>()
        {
            if (typeof(T) == typeof(string)) {
                return new(UnmanagedType.CustomMarshaler)
                {
                    TypeName = typeof(StringMarshaler).AssemblyQualifiedName
                };
            }
            if (typeof(T) == typeof(ModelIndex)) {
                return new(UnmanagedType.CustomMarshaler)
                {
                    TypeName = typeof(ModelIndexMarshaler).AssemblyQualifiedName
                };
            }
            if (typeof(T) == typeof(object)) {
                return new(UnmanagedType.CustomMarshaler)
                {
                    TypeName = typeof(ObjectMarshaler).AssemblyQualifiedName
                };
            }
            return new(typeof(T));
        }

        private static T FromPtr<T>(nint ptr)
        {
            if (typeof(T) == typeof(string))
                return (T)StringMarshaler.GetInstance("").MarshalNativeToManaged(ptr);
            if (typeof(T) == typeof(ModelIndex))
                return (T)ModelIndexMarshaler.GetInstance("").MarshalNativeToManaged(ptr);
            return (T)ObjectMarshaler.GetInstance("").MarshalNativeToManaged(ptr);
        }

        private static nint ToPtr<T>(T obj)
        {
            if (typeof(T) == typeof(string))
                return StringMarshaler.GetInstance("").MarshalManagedToNative(obj);
            if (typeof(T) == typeof(ModelIndex))
                return ModelIndexMarshaler.GetInstance("").MarshalManagedToNative(obj);
            return ObjectMarshaler.GetInstance("").MarshalManagedToNative(obj);
        }

        [TestMethod]
        public unsafe void StaticMethod()
        {
            var ptrToInt32 = Adapter.ResolveStaticMethod("System.Convert", "ToInt32", 0,
                [Param<int>(), Param<string>()]);
            Assert.AreNotEqual(nint.Zero, ptrToInt32);

            var ptrSafe = Adapter.ResolveSafeMethod(ptrToInt32, 0,
                [Param<object>(), Param<object>(), Param<string>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe);

            var fnToInt32 = (delegate* unmanaged[Stdcall]<nint, nint, nint>)ptrSafe;
            var ptrResult = fnToInt32(nint.Zero, ToPtr("42"));
            Assert.AreNotEqual(nint.Zero, ptrResult);

            var result = FromPtr<SafeReturn<int>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.AreEqual(42, result.Value);

            ptrResult = fnToInt32(nint.Zero, ToPtr("Foo"));
            Assert.AreNotEqual(nint.Zero, ptrResult);

            result = FromPtr<SafeReturn<int>>(ptrResult);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType<FormatException>(result.Exception);
        }

        [TestMethod]
        public unsafe void InstanceMethod()
        {
            StringBuilder obj = new();

            var ptrAppendFormat = Adapter.ResolveInstanceMethod(ToPtr(obj), "AppendFormat", 0,
                [Param<object>(), Param<string>(), Param<object>()]);
            Assert.AreNotEqual(nint.Zero, ptrAppendFormat);

            var ptrSafe = Adapter.ResolveSafeMethod(ptrAppendFormat, 0,
                [Param<object>(), Param<object>(), Param<string>(), Param<object>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe);

            var fnAppendFormat = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)ptrSafe;
            var ptrResult = fnAppendFormat(ToPtr(obj), ToPtr("{0}"), ToPtr((object)42));
            Assert.AreNotEqual(nint.Zero, ptrResult);

            var result = FromPtr<SafeReturn<StringBuilder>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.AreEqual(obj, result.Value);
            Assert.AreEqual("42", obj.ToString());

            ptrResult = fnAppendFormat(ToPtr(obj), ToPtr("{1}"), ToPtr((object)42));
            Assert.AreNotEqual(nint.Zero, ptrResult);

            result = FromPtr<SafeReturn<StringBuilder>>(ptrResult);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType<FormatException>(result.Exception);
        }

        [TestMethod]
        public unsafe void VoidMethod()
        {
            var ptrThrowIfNull = Adapter
                .ResolveStaticMethod("System.ArgumentNullException", "ThrowIfNull", 0,
                [Parameter.Void, Param<object>(), Param<string>()]);
            Assert.AreNotEqual(nint.Zero, ptrThrowIfNull);

            var ptrSafe = Adapter.ResolveSafeMethod(ptrThrowIfNull, 0,
                [Param<object>(), Param<object>(), Param<object>(), Param<string>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe);

            var fnThrowIfNull = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)ptrSafe;
            var ptrResult = fnThrowIfNull(nint.Zero, ToPtr((object)"foo"), ToPtr("bar"));
            Assert.AreNotEqual(nint.Zero, ptrResult);

            var result = FromPtr<SafeReturn<bool>>(ptrResult);
            Assert.IsNull(result.Exception);

            ptrResult = fnThrowIfNull(nint.Zero, nint.Zero, ToPtr("bar"));
            Assert.AreNotEqual(nint.Zero, ptrResult);

            result = FromPtr<SafeReturn<bool>>(ptrResult);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType<ArgumentNullException>(result.Exception);
        }

        [TestMethod]
        public unsafe void VirtualMethod()
        {
            var obj = new Primes() { Count = 1000 };

            var ptrData = Adapter.ResolveInstanceMethod(ToPtr(obj), "Data", 0,
                [Param<object>(), Param<ModelIndex>(), Param<int>()]);
            Assert.AreNotEqual(nint.Zero, ptrData);

            var ptrSafe = Adapter.ResolveSafeMethod(ptrData, 0,
                [Param<object>(), Param<object>(), Param<ModelIndex>(), Param<int>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe);

            var fnData = (delegate* unmanaged[Stdcall]<nint, nint, int, nint>)ptrSafe;
            var ptrResult = fnData(ToPtr(obj), ToPtr(new ModelIndex(42, 0)), 256);
            Assert.AreNotEqual(nint.Zero, ptrResult);

            var result = FromPtr<SafeReturn<object>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(191, ((Prime)result.Value).Value);
        }

        [TestMethod]
        public unsafe void HiddenMethod()
        {
            var obj = MethodInfo.GetCurrentMethod().GetType().GetProperty("Name");
            var ptrGetValue = Adapter.ResolveInstanceMethod(ToPtr(obj), "GetValue", 0,
                [Param<object>(), Param<object>(), Param<object[]>()]);
            Assert.AreNotEqual(nint.Zero, ptrGetValue);

            var ptrSafe = Adapter.ResolveSafeMethod(ptrGetValue, 0,
                [Param<object>(), Param<object>(), Param<object>(), Param<object[]>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe);

            var fnGetValue = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint>)ptrSafe;
            var ptrResult = fnGetValue(ToPtr(obj), ToPtr(MethodInfo.GetCurrentMethod()), nint.Zero);
            Assert.AreNotEqual(nint.Zero, ptrResult);

            var result = FromPtr<SafeReturn<object>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(nameof(HiddenMethod), (string)result.Value);
        }

        [TestMethod]
        public void Ensure_Strict_TypeBinding()
        {
            var objA = new string[8];
            var objB = new StringBuilder[8];

            var ptrSet_A = Adapter.ResolveInstanceMethod(ToPtr(objA), "Set", 0,
                [Parameter.Void, Param<int>(), Param<string>()]);
            Assert.AreNotEqual(nint.Zero, ptrSet_A);

            var ptrSet_B = Adapter.ResolveInstanceMethod(ToPtr(objB), "Set", 0,
                [Parameter.Void, Param<int>(), Param<StringBuilder>()]);
            Assert.AreNotEqual(nint.Zero, ptrSet_B);

            Assert.AreNotEqual(ptrSet_A, ptrSet_B);

            var ptrSafe_A = Adapter.ResolveSafeMethod(ptrSet_A, 0,
                [Param<object>(), Param<object>(), Param<int>(), Param<string>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe_A);

            var ptrSafe_B = Adapter.ResolveSafeMethod(ptrSet_B, 0,
                [Param<object>(), Param<object>(), Param<int>(), Param<StringBuilder>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe_B);

            Assert.AreNotEqual(ptrSafe_A, ptrSafe_B);
        }

        // Handle of the safe delegate that native code got for the given unsafe function pointer.
        private static GCHandle SafeDelegateHandle(nint unsafeFuncPtr)
        {
            var delegateRefs = (IDictionary)typeof(Adapter)
                .GetProperty("DelegateRefs", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
            var delegateInfo = delegateRefs[unsafeFuncPtr];
            var unsafeMethod = delegateInfo.GetType().GetField("Item2").GetValue(delegateInfo);
            var safeDelegateRefs = (IDictionary)typeof(Adapter)
                .GetProperty("SafeDelegateRefs", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
            var safeDelegateRef = safeDelegateRefs[unsafeMethod];
            return (GCHandle)safeDelegateRef
                .GetType().GetProperty("Handle").GetValue(safeDelegateRef);
        }

        [TestMethod]
        public void ResolveSafeMethod_DoesNotKeepHandlesOfFreedInstances()
        {
            // Instance methods of different objects have different unsafe function pointers,
            // which share one safe delegate.
            var objRefs = Enumerable.Range(0, 3)
                .Select(i => ToPtr(new StringBuilder($"{i}")))
                .ToArray();
            var safeHandles = objRefs
                .Select(objRef => {
                    var fn = Adapter.ResolveInstanceMethod(objRef, "ToString", 0,
                        [Param<string>()]);
                    Adapter.ResolveSafeMethod(fn, 0, [Param<object>(), Param<object>()]);
                    return SafeDelegateHandle(fn);
                })
                .ToArray();

            foreach (var objRef in objRefs)
                Adapter.FreeObjectRef(objRef);

            // The objects are gone, so at most one handle may still keep the shared delegate
            // alive. A freed handle no longer references its target.
            var allocated = safeHandles
                .Where(handle => handle.Target != null)
                .Select(GCHandle.ToIntPtr)
                .Distinct()
                .Count();
            Assert.IsLessThanOrEqualTo(1, allocated,
                $"{allocated} handles of the safe delegate outlive the freed objects");
        }

        [TestMethod]
        public unsafe void SameMethod_ResolvesTo_SamePointer()
        {
            StringBuilder objA = new();
            var objRefA = Adapter.GetRefPtrToObject(objA);
            Assert.AreNotEqual(nint.Zero, objRefA);

            StringBuilder objB = new();
            var objRefB = Adapter.GetRefPtrToObject(objB);
            Assert.AreNotEqual(nint.Zero, objRefB);
            Assert.AreNotEqual(objRefA, objRefB);

            var ptrAppend_A = Adapter.ResolveInstanceMethod(objRefA, "Append", 0,
                [Param<object>(), Param<int>()]);
            var ptrAppend_B = Adapter.ResolveInstanceMethod(objRefB, "Append", 0,
                [Param<object>(), Param<int>()]);
            Assert.AreNotEqual(ptrAppend_A, ptrAppend_B);

            var ptrSafe = Adapter.ResolveSafeMethod(ptrAppend_A, 0,
                [Param<object>(), Param<object>(), Param<int>()]);
            var ptrSafe_Dup = Adapter.ResolveSafeMethod(ptrAppend_B, 0,
                [Param<object>(), Param<object>(), Param<int>()]);
            Assert.AreNotEqual(nint.Zero, ptrSafe);
            Assert.AreEqual(ptrSafe, ptrSafe_Dup);

            var fnAppend = (delegate* unmanaged[Stdcall]<nint, int, nint>)ptrSafe;
            var ptrResult = fnAppend(ToPtr(objA), 42);
            Assert.AreNotEqual(nint.Zero, ptrResult);

            var result = FromPtr<SafeReturn<StringBuilder>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.AreEqual(objA, result.Value);
            Assert.AreEqual("42", objA.ToString());

            ptrResult = fnAppend(ToPtr(objB), 99);
            Assert.AreNotEqual(nint.Zero, ptrResult);

            result = FromPtr<SafeReturn<StringBuilder>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.AreEqual(objB, result.Value);
            Assert.AreEqual("99", objB.ToString());

            Adapter.FreeObjectRef(objRefB);

            ptrResult = fnAppend(ToPtr(objA), 42);
            Assert.AreNotEqual(nint.Zero, ptrResult);

            result = FromPtr<SafeReturn<StringBuilder>>(ptrResult);
            Assert.IsNull(result.Exception);
            Assert.AreEqual(objA, result.Value);
            Assert.AreEqual("4242", objA.ToString());

            Adapter.FreeObjectRef(objRefA);
        }

        [TestMethod]
        public void ResolveSafeMethod_YieldsCorrectFuncPtrs()
        {
            var a = new[] { "this is a string", "array" };
            var b = new StringBuilder[] { new("this is a string builder"), new("array") };

            var aPtr = Adapter.GetRefPtrToObject(a);
            Assert.AreNotEqual(nint.Zero, aPtr);

            var bPtr = Adapter.GetRefPtrToObject(b);
            Assert.AreNotEqual(nint.Zero, bPtr);

            var aFnGet = Adapter.ResolveInstanceMethod(aPtr, "Get", 2,
                FuncParams(typeof(string), typeof(int)));
            Assert.AreNotEqual(nint.Zero, aFnGet);

            var bFnGet = Adapter.ResolveInstanceMethod(bPtr, "Get", 2,
                FuncParams(typeof(StringBuilder), typeof(int)));
            Assert.AreNotEqual(nint.Zero, bFnGet);

            var aSafeGet = Adapter.ResolveSafeMethod(aFnGet, 2,
                SafeFuncParams(typeof(string), typeof(int)));
            Assert.AreNotEqual(nint.Zero, aSafeGet);

            var bSafeGet = Adapter.ResolveSafeMethod(bFnGet, 2,
                SafeFuncParams(typeof(StringBuilder), typeof(int)));
            Assert.AreNotEqual(nint.Zero, bSafeGet);

            var aFnGetDuplicate = Adapter.ResolveInstanceMethod(aPtr, "Get", 2,
                FuncParams(typeof(string), typeof(int)));
            Assert.AreNotEqual(nint.Zero, aFnGetDuplicate);
            Assert.AreEqual(aFnGet, aFnGetDuplicate);

            var aSafeGetDuplicate = Adapter.ResolveSafeMethod(aFnGetDuplicate, 2,
                SafeFuncParams(typeof(string), typeof(int)));
            Assert.AreNotEqual(nint.Zero, aSafeGetDuplicate);
            Assert.AreEqual(aSafeGet, aSafeGetDuplicate);

            Adapter.FreeObjectRef(aPtr);
            Adapter.FreeObjectRef(bPtr);
        }
    }
}
