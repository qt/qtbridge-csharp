// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick

Window {
    visible: false
    required property int foo
    required property var bar

    function assert(ok, msg) {
        if (ok)
            return true
        console.error("Error: " + msg)
        return false
    }

    Component.onCompleted: {
        console.log("=== TEST: [" + QmlLoad.testName + "]")
        console.log("foo: [" + foo + "]")
        console.log("bar: [" + bar + "]")
        console.log("Evaluating: [" + QmlLoad.testExpr + "]")
        if (eval(QmlLoad.testExpr)) {
            console.log("Result: PASS")
            QmlLoad.result = true
        } else {
            console.log("Result: FAIL")
            QmlLoad.result = false
        }
    }
}
