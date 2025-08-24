/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
import QtQuick

Window {
    id: window; width: 640; height: 480; visible: true;
    title: "Primes: object model proxy"

    PrimeFactory {
        id: primeFactory
    }

    GridView {
        id: primeGrid; model: primeFactory.getNPrimes(1000); delegate: primeDelegate
        anchors.fill: parent; cellWidth: parent.width / 10; cellHeight: parent.height / 10
    }
    Component {
        id: primeDelegate
        Rectangle {
            id: wrapper
            required property QtObject item
            width: window.width / 10; height: window.height / 10;
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)

            Text {
                text: item.value
                anchors.centerIn: parent; font.pixelSize: 18
            }
            Text {
                text: "#" + item.index.toString()
                anchors.top: parent.top; anchors.left: parent.left; anchors.margins: 2
            }
        }
    }
}
