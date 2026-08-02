// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            Assert.AreNotEqual(aSafeGet, bSafeGet);

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
