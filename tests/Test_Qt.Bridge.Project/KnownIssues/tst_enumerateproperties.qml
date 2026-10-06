// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

// Run on its own, since the known issue aborts the process.
Item {
    Inspected { id: inspected }

    TestCase {
        name: "EnumerateProperties"

        function test_objectKeys() {
            const keys = Object.keys(inspected)
            verify(keys.includes("value"), JSON.stringify(keys))
        }
    }
}
