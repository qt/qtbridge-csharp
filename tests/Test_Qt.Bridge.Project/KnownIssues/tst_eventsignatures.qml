// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

Item {
    Signaller { id: signaller }

    SignalSpy { id: signalledSpy; target: signaller; signalName: "signalled" }
    SignalSpy { id: countedSpy; target: signaller; signalName: "counted" }
    SignalSpy { id: movedSpy; target: signaller; signalName: "moved" }

    TestCase {
        name: "EventSignatures"

        function test_controlEventArgs() {
            signaller.raiseSignalled()
            compare(signalledSpy.count, 1)
        }

        function test_action() {
            signaller.raiseCounted(3)
            compare(countedSpy.count, 1)
            compare(countedSpy.signalArguments[0][0], 3)
        }

        // Whether the signal also passes the sender is up to the fix; the value comes last.
        function test_eventHandlerOfValue() {
            signaller.raiseMoved(5)
            compare(movedSpy.count, 1)
            const args = movedSpy.signalArguments[0]
            compare(args[args.length - 1], 5)
        }
    }
}
