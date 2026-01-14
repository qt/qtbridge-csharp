// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import Application

Window {
    id: window; width: 640; height: 480; visible: true;
    title: "Primes! - Delegate creates C# object"

    GridView {
        model: 1000;
        delegate: Rectangle {
            required property int index
            width: window.width / 10; height: window.height / 10;
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)

            Prime {
                id : prime
                n: index + 1
            }

            Text {
                text: prime.value
                anchors.centerIn: parent; font.pixelSize: parent.width / 4
            }

            Text {
                text: "#" + prime.n.toString()
                anchors.top: parent.top; anchors.left: parent.left; anchors.margins: 2
                font.pixelSize: parent.width / 6
            }
        }
        anchors.fill: parent; cellWidth: parent.width / 10; cellHeight: parent.height / 10
    }
}
