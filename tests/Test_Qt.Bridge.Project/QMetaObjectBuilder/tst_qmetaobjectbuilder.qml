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

    InheritedMethodsObject {
        id: inherited
    }

    TestCase {
        name: "tst_qmetaobjectbuilder"
        when: true

        function test_metadata_method_dispatch_with_signals() {
            compare(fixture.value(), 42)
            compare(fixture.add(19, 23), 42)
        }

        // Own and inherited methods come from different modules, whose metadata tokens overlap.
        function test_metadata_methods_from_other_modules() {
            const colliding = inherited.collidingMethods()
            verify(colliding.length > 0, "No own method shares its token with an inherited one")
            for (const pair of colliding.split(",")) {
                const [own, base] = pair.split(":").map(n => n[0].toLowerCase() + n.slice(1))
                verify(typeof inherited[own] === "function", own + " is missing")
                verify(typeof inherited[base] === "function", base + " is missing")
                compare(inherited[own](), parseInt(own.slice(3)))

                // The inherited method must not reach the own one with the same token.
                const ownCalls = inherited.ownCalls()
                inherited[base]()
                compare(inherited.ownCalls(), ownCalls, base + " called " + own)
            }
        }

        // Get() of arrays is provided by the runtime and has no metadata token of its own.
        function test_metadata_array_methods() {
            const items = fixture.items()
            compare(items.get(0).value, 1)
            compare(items.get(1).value, 2)
        }

        function test_metadata_event_delivery() {
            fixture.raiseEvents()
            tryCompare(root, "changedCount", 1)
            tryCompare(root, "updatedCount", 1)
        }
    }
}
