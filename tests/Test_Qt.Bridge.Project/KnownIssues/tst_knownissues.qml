// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

// One test function per known issue; Test_KnownIssues.cs evaluates each one. Functions named
// test_control... check what works, so that a failing known-issue function is not mistaken for
// a broken test run.
Item {
    Notifier { id: notifier }
    Clock { id: clock }
    Customer { id: customerAll }
    Customer { id: customerEach }

    SignalSpy { id: plainSpy; target: notifier; signalName: "plain" }
    SignalSpy { id: customSpy; target: notifier; signalName: "custom" }

    // Bindings re-evaluate only when the notify signal is emitted; reading the property
    // directly would always return the current value from .NET.
    property string nameOfAll: customerAll.name
    property string nameOfEach: customerEach.name
    SignalSpy { id: allNameSpy; target: customerAll; signalName: "nameChanged" }
    SignalSpy { id: allRevisionSpy; target: customerAll; signalName: "revisionChanged" }
    SignalSpy { id: eachNameSpy; target: customerEach; signalName: "nameChanged" }
    SignalSpy { id: eachRevisionSpy; target: customerEach; signalName: "revisionChanged" }

    TestCase {
        name: "KnownIssues"

        function test_controlCustomEventArgs() {
            notifier.raiseCustom()
            compare(customSpy.count, 1)
        }

        function test_emptyEventArgs() {
            notifier.raisePlain()
            compare(plainSpy.count, 1)
        }

        function test_dateTimeMilliseconds() {
            compare(clock.utc.toISOString(), "2026-03-29T12:34:56.789Z")
        }

        function test_dateTimeLocalKind() {
            compare(clock.local.toISOString(), clock.localAsIso)
        }

        function test_dateTimeFromQml() {
            const date = new Date(Date.UTC(2026, 2, 29, 12, 34, 56, 789))
            compare(clock.describe(date), date.toISOString())
        }

        function test_controlPropertyChangedEach() {
            customerEach.reloadEach()
            compare(eachNameSpy.count, 1)
            compare(eachRevisionSpy.count, 1)
            compare(nameOfEach, "After")
        }

        function test_propertyChangedNullName() {
            customerAll.reloadAll()
            compare(allNameSpy.count, 1)
            compare(allRevisionSpy.count, 1)
            compare(nameOfAll, "After")
        }
    }
}
