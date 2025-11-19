/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
import QtQuick
import Application

Window {
    id: window; width: 640; height: 480; visible: true;
    title: "Primes! - Model from QAIM-based C# class"

    GridView {
        model: Primes { count: 1000 }
        delegate: Rectangle {
            required property int n
            required property int value
            width: window.width / 10; height: window.height / 10;
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)

            Text {
                text: value
                anchors.centerIn: parent; font.pixelSize: parent.width / 4
            }

            Text {
                text: "#" + n.toString()
                anchors.top: parent.top; anchors.left: parent.left; anchors.margins: 2
                font.pixelSize: parent.width / 6
            }
        }

        anchors.fill: parent; cellWidth: parent.width / 10; cellHeight: parent.height / 10
    }
}
