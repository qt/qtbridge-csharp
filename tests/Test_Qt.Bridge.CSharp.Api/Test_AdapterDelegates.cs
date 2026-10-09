// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Api
{
    [TestClass]
    public class Test_AdapterDelegates
    {
        // Native callers pass strings in buffers they allocate and hand over, and release the
        // strings they receive with LocalFree(). StringMarshaler follows that protocol; LPWStr
        // never frees the buffers it receives, and allocates the ones it returns with
        // CoTaskMemAlloc().
        [TestMethod]
        public void StringsFromNativeCallers_AreMarshaledWithStringMarshaler()
        {
            var mismatches = new List<string>();
            foreach (var type in typeof(Adapter.Delegates).GetNestedTypes()) {
                // The adapter calls this one, so the runtime owns the strings it passes.
                if (!type.IsSubclassOf(typeof(Delegate))
                    || type == typeof(Adapter.Delegates.NativeEventHandler)) {
                    continue;
                }
                var invoke = type.GetMethod("Invoke");
                foreach (var param in invoke.GetParameters().Append(invoke.ReturnParameter)) {
                    if (param.ParameterType != typeof(string))
                        continue;
                    var marshalAs = param.GetCustomAttribute<MarshalAsAttribute>();
                    if (marshalAs?.Value != UnmanagedType.CustomMarshaler
                        || marshalAs.MarshalTypeRef != typeof(StringMarshaler)) {
                        mismatches.Add($"{type.Name}({param.Name ?? "return"})");
                    }
                }
            }
            Assert.AreEqual(string.Empty, string.Join(", ", mismatches));
        }
    }
}
