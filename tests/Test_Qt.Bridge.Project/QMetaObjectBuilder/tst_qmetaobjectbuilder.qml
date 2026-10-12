// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

Item {
    id: root

    property int changedCount: 0
    property int updatedCount: 0

    MetadataObject {
        id: fixture
        onChanged: root.changedCount++
        onUpdated: root.updatedCount++
    }

    TestCase {
        name: "tst_qmetaobjectbuilder"
        when: true

        function test_metadata_method_dispatch_with_signals() {
            compare(fixture.value(), 42)
            compare(fixture.add(19, 23), 42)
        }

        function test_metadata_event_delivery() {
            fixture.raiseEvents()
            tryCompare(root, "changedCount", 1)
            tryCompare(root, "updatedCount", 1)
        }
    }
}
