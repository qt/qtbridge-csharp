// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

// Run on its own, since the known issue aborts the process with source code exports.
Item {
    Parcel { id: parcel }

    TestCase {
        name: "StructProperty"

        function test_read() {
            compare(parcel.size.width, 3)
            compare(parcel.size.height, 4)
        }

        // Like a property of a QML value type, such as point.x, writes back to the object.
        function test_writeBack() {
            parcel.size.width = 10
            compare(parcel.size.width, 10)
        }
    }
}
