/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Adapter
{
    using Qt.DotNet;
    using static Qt.DotNet.Adapter;

    [TestClass]
    public sealed class Test_QDotNetAdapter
    {
        [TestMethod]
        public void BuiltInTest()
        {
            var ctorPtr = ResolveConstructor(1, [new Parameter("FooLib.Foo, FooLib")]);

            var ctor = GetMember(ctorPtr) as ConstructorInfo;
            Debug.Assert(ctor != null, nameof(ctor) + " is null");
            var objRef = GetRefPtrToObject(ctor.Invoke([]));

            var getTypePtr = ResolveInstanceMethod(
                objRef, "GetType", 1, [new Parameter("System.Type")]);
            Assert.IsNotNull(getTypePtr);

            var getBarPtr = ResolveInstanceMethod(
                objRef, "get_Bar", 1, [new Parameter(UnmanagedType.LPWStr)]);
            Assert.IsNotNull(getBarPtr);

            var setBarPtr = ResolveInstanceMethod(
                objRef, "set_Bar", 1, [new(), new Parameter(UnmanagedType.LPWStr)]);
            Assert.IsNotNull(setBarPtr);

            AddEventHandler(
                objRef,
                "PropertyChanged",
                new IntPtr(42),
                TestNativeEventHandler);

            for (var i = 0; i < 1000; ++i) {
                var str = (GetMember(getBarPtr) as MethodBase)
                    ?.Invoke(GetObjectRefFromPtr(objRef).Target, []) as string;
                str += "hello";
                (GetMember(setBarPtr) as MethodBase)
                    ?.Invoke(GetObjectRefFromPtr(objRef).Target, [str]);
            }

            RemoveAllEventHandlers(objRef);
            FreeObjectRef(objRef);
            FreeTypeRef("FooLib.Foo, FooLib");

            Stats(out var objectCount, out var delegateCount, out var eventCount);

            Assert.AreEqual(0, objectCount);
            Assert.AreEqual(0, delegateCount);
            Assert.AreEqual(0, eventCount);
        }

        private static void TestNativeEventHandler(IntPtr context, string eventName,
            object senderObj, object argsObj)
        {
            var senderRef = GetRefPtrToObject(senderObj);
            var argsRef = GetRefPtrToObject(argsObj);

            var getTypePtr = ResolveInstanceMethod(
                argsRef, "GetType", 1, [new Parameter("System.Type")]);
            if (eventName == "PropertyChanged") {
                var typeObj = (GetMember(getTypePtr) as MethodBase)
                    ?.Invoke(GetObjectRefFromPtr(argsRef).Target, []);
                var typeRef = GetRefPtrToObject(typeObj);

                var getFullNamePtr = ResolveInstanceMethod(
                    typeRef, "get_FullName", 1, [new Parameter(UnmanagedType.LPWStr)]);
                var argsTypeName = (GetMember(getFullNamePtr) as MethodBase)
                    ?.Invoke(GetObjectRefFromPtr(typeRef).Target, [])
                    as string;

                if (argsTypeName == "System.ComponentModel.PropertyChangedEventArgs") {

                    var propChangeRef = AddObjectRef(argsRef);

                    var getPropertyNamePtr = ResolveInstanceMethod(
                        propChangeRef, "get_PropertyName", 1,
                        [
                            new Parameter(UnmanagedType.LPWStr)
                        ]);
                    var propName = (GetMember(getPropertyNamePtr) as MethodBase)
                        ?.Invoke(GetObjectRefFromPtr(propChangeRef).Target, [])
                        as string;

                    if (propName == "Bar") {
                        var getBarPtr = ResolveInstanceMethod(
                            senderRef, "get_Bar", 1, [new Parameter(UnmanagedType.LPWStr)]);
                        var str = (GetMember(getBarPtr) as MethodBase)
                            ?.Invoke(GetObjectRefFromPtr(senderRef).Target, [])
                            as string;
                        Debug.Assert(str != null, nameof(str) + " is null");
                        Console.WriteLine($"BAR CHANGED!!! [{str.Length / "hello".Length}x hello]");
                    }
                    FreeObjectRef(typeRef);
                    FreeObjectRef(propChangeRef);
                }
            }
            FreeObjectRef(argsRef);
            FreeObjectRef(senderRef);
        }
    }
}
