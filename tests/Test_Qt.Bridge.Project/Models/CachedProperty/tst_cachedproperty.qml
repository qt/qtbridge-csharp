// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

Item {
    id: root

    NotifyingCollection {
        id: notifying
    }

    FreedMemory {
        id: freedMemory
    }

    // Reports the object references the adapter holds; each live wrapper holds one.
    AdapterReferences {
        id: references
    }

    TestCase {
        name: "tst_cachedproperty"
        when: true

        // Reads the collection without keeping a reference to it, so nothing on
        // the JavaScript side holds on to the object once the function returns.
        function itemCount() {
            return notifying.items.rowCount()
        }

        function cleanup() {
            freedMemory.release()
        }

        function test_collection_property_survives_garbage_collection() {
            compare(itemCount(), 2)
            const withWrapper = references.objectRefCount()

            // Forces garbage collection and deleting the unreferenced wrapper.
            wait(0)
            gc()
            wait(0)
            verify(references.objectRefCount() < withWrapper)
            freedMemory.overwrite()

            // Reading again must return a live wrapper, not the deleted one.
            verify(notifying.items !== null, "The collection property reads null")
            compare(itemCount(), 2)
            compare(references.objectRefCount(), withWrapper)
        }
    }
}
