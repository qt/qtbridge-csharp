/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

import QtQuick
import QtQuick.Controls
import Application

Window {
    id: window
    width: 640; height: 480; visible: true
    title: "Primes! - ObservableCollection<T> live model"

    ObservablePrimes {
        id: vm
    }

    Row {
        spacing: 8
        anchors.margins: 8
        anchors.top: parent.top
        anchors.horizontalCenter: parent.horizontalCenter
        Button {
            text: "Add";
            onClicked: vm.addNext()
        }
        Button {
            text: "Remove";
            onClicked: vm.removeLast()
        }
        Button {
            text: "Replace";
            onClicked: vm.replaceFirst()
        }
        Button {
            text: "Move";
            onClicked: vm.moveFirstToEnd()
        }
        Button {
            text: "Reset";
            onClicked: vm.reset()
            }
    }

    GridView {
        anchors.fill: parent
        anchors.margins: 8
        anchors.topMargin: 50
        cellWidth: width / 10
        cellHeight: height / 10
        model: vm.items
        delegate: Rectangle {
            required property QtObject item
            required property int n
            required property int value
            width: GridView.view.cellWidth
            height: GridView.view.cellHeight
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)

            Text { text: value; anchors.centerIn: parent; font.pixelSize: parent.width / 4 }
            Text {
                text: "#" + n
                anchors.top: parent.top; anchors.left: parent.left; anchors.margins: 2
                font.pixelSize: parent.width / 6
            }
        }
    }
}
