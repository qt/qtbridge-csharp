// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_qmltomanageddelegates";

    Callback { id: callback }

    function test_delegate_param_callback_int_return() {
        compare(callback.invokeSingle(function(value) {
            return value + 5;
        }), 47);
    }

    function test_void_delegate() {
        // The lambda captures 'received' by closure; since the call is synchronous,
        // received is set before invokeVoid returns.
        var received = -1;
        compare(callback.invokeVoid(function(value) {
            received = value;
        }), 0);
        compare(received, 42);
    }

    function test_multiple_invocations() {
        // Same delegate invoked three times: 1*2 + 10*2 + 100*2 = 222
        compare(callback.invokeThrice(function(value) {
            return value * 2;
        }), 222);
    }

    function test_multi_param_delegate() {
        compare(callback.invokeCombiner(function(a, b) {
            return a * b;
        }), 42);
    }

    function test_no_param_delegate() {
        compare(callback.invokeProvider(function() {
            return 99;
        }), 99);
    }

    function test_js_exception_propagates() {
        // The thrown JS Error is recorded by ScriptDelegateContext and surfaces
        // as an InvalidOperationException in managed code; InvokeThrowing catches
        // it and returns -2.
        compare(callback.invokeThrowing(function(value) {
            throw new Error("test error from QML");
        }), -2);
    }

    function test_object_as_delegate_arg() {
        // C# creates Point{X=3,Y=4} and passes it to the lambda via toScriptValue;
        // QML reads x/y off the resulting QObject and returns x*x + y*y = 25.
        compare(callback.invokeWithPoint(function(p) {
            return p.x * p.x + p.y * p.y;
        }), 25);
    }

    function test_object_as_delegate_return() {
        // C# passes Point{X=7,Y=8} to the lambda; QML returns it unchanged.
        // fromScriptResult reconstructs the managed Point; C# reads X+Y = 15.
        compare(callback.invokeWithPointRoundtrip(function(p) {
            return p;
        }), 15);
    }

    function test_null_delegate() {
        // Passing null instead of a callable: fromScriptDelegate returns nullptr,
        // the C# side receives a null delegate, and the null-coalescing fallback fires.
        compare(callback.invokeSingle(null), -1);
    }

    function test_bcl_action_delegate() {
        // Action<int> is a BCL void delegate; same closure trick as test_void_delegate.
        var received = -1;
        compare(callback.invokeAction(function(value) {
            received = value;
        }), 0);
        compare(received, 42);
    }

    function test_bcl_func_delegate() {
        // Func<int,int> is a BCL int->int delegate.
        compare(callback.invokeFunc(function(value) {
            return value + 5;
        }), 47);
    }
}
