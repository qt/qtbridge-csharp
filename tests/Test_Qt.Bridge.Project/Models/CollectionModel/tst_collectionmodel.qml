// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

Item {
    id: root

    property var numbersFromItemRole: []
    property var peopleFromItemRole: []
    property var namesFromPropertyRole: []
    property var agesFromPropertyRole: []

    CollectionModels {
        id: fixture
    }

    Instantiator {
        model: fixture.numbers
        delegate: QtObject {
            Component.onCompleted: root.numbersFromItemRole =
                root.numbersFromItemRole.concat([model.item])
        }
    }

    Instantiator {
        model: fixture.people
        delegate: QtObject {
            Component.onCompleted: {
                root.peopleFromItemRole = root.peopleFromItemRole.concat([model.item])
                root.namesFromPropertyRole = root.namesFromPropertyRole.concat([model.name])
                root.agesFromPropertyRole = root.agesFromPropertyRole.concat([model.age])
            }
        }
    }

    TestCase {
        name: "tst_collectionmodel"
        when: true

        function test_int_array_item_role() {
            tryCompare(root.numbersFromItemRole, "length", 3)
            compare(root.numbersFromItemRole, [2, 3, 5])
        }

        function test_dto_list_item_and_property_roles() {
            tryCompare(root.peopleFromItemRole, "length", 2)
            tryCompare(root.namesFromPropertyRole, "length", 2)
            tryCompare(root.agesFromPropertyRole, "length", 2)

            compare(root.peopleFromItemRole[0].name, "Ada")
            compare(root.peopleFromItemRole[0].age, 36)
            compare(root.peopleFromItemRole[1].name, "Grace")
            compare(root.peopleFromItemRole[1].age, 85)
            compare(root.namesFromPropertyRole, ["Ada", "Grace"])
            compare(root.agesFromPropertyRole, [36, 85])
        }
    }
}
