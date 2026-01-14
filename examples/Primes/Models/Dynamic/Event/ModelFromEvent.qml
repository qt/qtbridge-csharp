// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import Application

Window {
    id: window; width: 640; height: 480; visible: true;
    title: "Primes! - Model from C# event"
    Component.onCompleted: primeFactory.createNPrimes(1000)

    PrimeFactory {
        id: primeFactory
        onPrimesCreated: args => gridPrimes.model = args.primes
    }

    GridView {
        id: gridPrimes
        model: []
        delegate: Rectangle {
            required property QtObject item
            required property int n
            width: window.width / 10; height: window.height / 10;
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)

            Text {
                text: item.value  // accessing the 'value' property via the 'item' object
                anchors.centerIn: parent; font.pixelSize: parent.width / 4
            }

            Text {
                text: "#" + n.toString() // accessing the 'n' property directly
                anchors.top: parent.top; anchors.left: parent.left; anchors.margins: 2
                font.pixelSize: parent.width / 6
            }
        }
        anchors.fill: parent; cellWidth: parent.width / 10; cellHeight: parent.height / 10
    }
}
