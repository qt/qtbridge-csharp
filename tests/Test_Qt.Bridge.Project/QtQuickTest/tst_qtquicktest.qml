// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_qtquicktest"

    FortyTwo { id: fortyTwo }

    function test_fortytwo() {
        console.log("Hello World from QML!")
        compare(fortyTwo.value(), 42)
    }
}
