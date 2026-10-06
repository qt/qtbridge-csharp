// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

// Uses the QML names of source code exports, must pass unchanged for type metadata exports.
Item {
    Library {
        id: library
    }

    SignalSpy { id: urlPathSpy; target: library; signalName: "urlPathChanged" }
    SignalSpy { id: newSpy; target: library; signalName: "newChanged" }
    SignalSpy { id: countSpy; target: library; signalName: "z_countChanged" }
    SignalSpy { id: urlSpy; target: library; signalName: "urlChanged" }
    SignalSpy { id: deleteSpy; target: library; signalName: "delete_" }

    TestCase {
        name: "ExportModeParity"

        function init() {
            for (const spy of [urlPathSpy, newSpy, countSpy, urlSpy, deleteSpy])
                spy.clear()
        }

        function test_properties() {
            library.urlPath = "qt.io"
            compare(library.urlPath, "qt.io")
            compare(urlPathSpy.count, 1)

            library.new_ = 3
            compare(library.new_, 3)
            compare(newSpy.count, 1)

            library.z_count = 5
            compare(library.z_count, 5)
            compare(countSpy.count, 1)

            library.x = 7
            compare(library.x, 7)
        }

        function test_methods() {
            compare(library.getHttpResponse("qt.io", 2), "qt.io:2")
            verify(library.isOk())
            compare(library.a(), 1)

            library.x = 4
            library.switch_()
            compare(library.x, -4)

            library.z_count = 9
            library.z_reset()
            compare(library.z_count, 0)

            compare(library.item(21), 42)
            library.setItem(1, 2)
            compare(library.x, 3)
        }

        function test_signals() {
            library.raiseEvents()
            compare(urlSpy.count, 1)
            compare(deleteSpy.count, 1)
        }

        // QML looks up Type.key only for keys that start with an upper-case letter, so _Hidden,
        // exported as z_Hidden, is only accessible through the enum: Shade.Values.z_Hidden.
        function test_enum() {
            compare(Shade.Red, 0)
            compare(Shade.Green, -1)
            compare(Shade.Default, 7)
            compare(Shade.Values.z_Hidden, 5)
            compare(library.shade, Shade.Green)
        }
    }
}
