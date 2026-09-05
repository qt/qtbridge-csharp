// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ColumnLayout {
    id: root

    required property string label
    required property var model

    anchors.fill: parent
    spacing: 6

    Label {
        text: root.label
        font.bold: true
    }

    Frame {
        Layout.fillWidth: true
        Layout.fillHeight: true
        padding: 6

        ListView {
            anchors.fill: parent
            clip: true
            model: root.model
            spacing: 4

            delegate: Rectangle {
                id: row

                required property int index
                required property int item

                width: ListView.view.width
                height: 30
                radius: 4
                color: row.index % 2 === 0 ? "#ffffff" : "#f6f7f8"
                border.color: "#e3e5e8"

                Label {
                    anchors.verticalCenter: parent.verticalCenter
                    anchors.left: parent.left
                    anchors.leftMargin: 10
                    text: "#" + (row.index + 1) + ": " + row.item
                }
            }
        }
    }
}
