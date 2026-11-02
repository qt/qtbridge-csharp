// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

// Run on its own, since the known issue aborts the process with source code exports.
Item {
    Parcel { id: parcel }

    TestCase {
        name: "NullableProperty"

        function test_nullableProperty() {
            compare(parcel.count, 5)
            parcel.count = 7
            compare(parcel.count, 7)
            parcel.count = null
            compare(parcel.count, null)
        }
    }
}
