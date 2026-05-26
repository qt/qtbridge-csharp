// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_property"

    Prime { id: prime }

    function test_A_get_0s() {
        compare(prime.n, 0)
        compare(prime.value, 0)
    }

    function test_B_set_n25() {
        prime.n = 25
        compare(prime.n, 25)
    }

    function test_C_get_value97() {
        compare(prime.value, 97)
    }

    function test_D_write_value_is_readonly() {
        try {
            prime.value = 999
            fail("Seting value of read-only property")
        } catch (e) {
        }
    }
}
