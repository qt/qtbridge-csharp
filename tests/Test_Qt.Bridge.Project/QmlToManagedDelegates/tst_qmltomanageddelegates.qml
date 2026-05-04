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
}
