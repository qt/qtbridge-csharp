// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_callmethod"

    PrimeFactory { id: primeFactory }

    function test_callMethod() {
        compare(primeFactory.getNthPrime(25), 97)
    }
}
