/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_structureddata"

    StructuredData {
        id: data
    }

    // simple roundtrip, single person
    function test_person_roundtrip()
    {
        // Host -> QML
        var p = data.createSamplePerson()
        compare(p.name, "Alice")
        compare(p.age, 42)

        // Mutation in QML
        p.name = "Bob"
        p.age = 21

        // QML -> Host
        data.acceptPerson(p)

        // Host -> QML (Roundtrip)
        var back = data.lastPerson
        compare(back.name, "Bob")
        compare(back.age, 21)
    }

    // roundtrip with nested structure (Team + Member list)
    function test_team_roundtrip()
    {
        // Host -> QML
        var intitialTeam = data.createSampleTeam()
        compare(intitialTeam.name, "Awesome Team")

        var intitialMembers = intitialTeam.members;
        compare(intitialMembers.count, 2)
        compare(intitialMembers.item(0).name, "Alice")
        compare(intitialMembers.item(1).name, "Bob")

        // Mutation in QML -> Host
        var newPerson = Qt.createQmlObject(
            'import Application 1.0; Person { name: "Eve"; age: 60 }',
            this
        )
        intitialTeam.name = "Renamed Team"
        intitialMembers.add(newPerson)
        data.acceptTeam(intitialTeam)

        // Host -> QML (Roundtrip)
        var updatedTeam = data.lastTeam
        compare(updatedTeam.name, "Renamed Team")

        var updatedMembers = updatedTeam.members;
        compare(updatedMembers.count, 3)
        compare(updatedMembers.item(0).name, "Alice")
        compare(updatedMembers.item(1).name, "Bob")
        compare(updatedMembers.item(2).name, "Eve")
        compare(updatedMembers.item(2).age, 60)
    }
}
