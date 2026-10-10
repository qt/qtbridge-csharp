// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

Item {
    id: root

    property int metadataDelegatesCreated: 0
    property int metadataPeopleCount: metadataItems.people.count

    MetadataObservable {
        id: metadataItems
    }

    SourceObservable {
        id: sourceItems
    }

    Instantiator {
        id: metadataView
        model: metadataItems.items
        delegate: QtObject {}
    }

    property int sourceScriptedCount: sourceItems.scripted.count
    property int metadataScriptedCount: metadataItems.scripted.count

    Instantiator {
        id: sourceScriptedView
        model: sourceItems.scripted
        delegate: QtObject {
            property string name: model.name
        }
    }

    Instantiator {
        id: metadataScriptedView
        model: metadataItems.scripted
        delegate: QtObject {
            property string name: model.name
        }
    }

    Instantiator {
        id: peopleView
        model: metadataItems.people
        delegate: QtObject {
            Component.onCompleted: root.metadataDelegatesCreated++
        }
    }

    TestCase {
        name: "tst_observablemodel"
        when: true

        // Any change to a collection must reach the view as a delta. Resetting
        // the model recreates the delegates for all elements each time.
        function test_metadata_collection_changes_keep_existing_delegates() {
            for (let i = 0; i < 10; ++i)
                metadataItems.addPerson("person" + i)
            compare(peopleView.count, 10)
            compare(root.metadataDelegatesCreated, 10)

            metadataItems.removeFirstPerson()
            compare(peopleView.count, 9)
            compare(root.metadataDelegatesCreated, 10)
        }

        // Bindings to the count of a collection must follow its changes, as they do in source
        // builds.
        function test_metadata_collection_count_notifies() {
            const count = metadataItems.people.count
            compare(root.metadataPeopleCount, count)

            metadataItems.addPerson("added")
            compare(root.metadataPeopleCount, count + 1)

            metadataItems.removeFirstPerson()
            compare(root.metadataPeopleCount, count)

            metadataItems.clearPeople()
            compare(root.metadataPeopleCount, 0)
        }

        function names(view) {
            let result = []
            for (let i = 0; i < view.count; ++i)
                result.push(view.objectAt(i).name)
            return result.join(",")
        }

        // Changes that cannot be mapped to a delta must still reach the view and the count, as a
        // reset; changes that can be mapped still apply as deltas.
        function checkScriptedChanges(people, view, count) {
            people.addUnpositioned("a")
            compare(names(view), "a")
            compare(count(), 1)

            people.addOutOfRange("b")
            compare(names(view), "a,b")
            compare(count(), 2)

            people.addWithCustomArgs("c")
            compare(names(view), "a,b,c")
            compare(count(), 3)

            people.replaceFirstUnpositioned("d")
            compare(names(view), "d,b,c")

            people.removeFirstUnpositioned()
            compare(names(view), "b,c")
            compare(count(), 2)

            people.moveFirstToLast()
            compare(names(view), "c,b")
            compare(count(), 2)
        }

        function test_source_collection_scripted_changes() {
            checkScriptedChanges(sourceItems.scripted, sourceScriptedView,
                                 () => root.sourceScriptedCount)
        }

        function test_metadata_collection_scripted_changes() {
            checkScriptedChanges(metadataItems.scripted, metadataScriptedView,
                                 () => root.metadataScriptedCount)
        }

        // The collection reaches QML through a member of a metadata type.
        function test_metadata_changes_reach_the_view() {
            for (let i = 0; i < 3; ++i)
                metadataItems.add("item" + i)
            compare(metadataView.count, 3)

            metadataItems.removeFirst()
            compare(metadataView.count, 2)
        }
    }
}
