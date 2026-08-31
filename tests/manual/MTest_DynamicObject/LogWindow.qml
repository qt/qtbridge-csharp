// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

Window {
    required property string logText
    signal clearRequested

    width: 480
    height: 600
    visible: false
    title: "Log"
    color: "#f3f4f6"

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 20
        spacing: 12

        RowLayout {
            Layout.fillWidth: true
            spacing: 10

            Label {
                text: "Session log"
                font.bold: true
                Layout.fillWidth: true
            }
            Button {
                leftPadding: 10
                rightPadding: 10
                text: "Clear"
                onClicked: clearRequested()
            }
        }

        ScrollView {
            id: logScroll
            Layout.fillWidth: true
            Layout.fillHeight: true
            clip: true

            TextArea {
                id: logArea
                readOnly: true
                selectByMouse: true
                text: logText
                wrapMode: Text.WrapAnywhere
                font.family: "Consolas"
                font.pixelSize: 12
                color: "#1c1e21"
                onTextChanged: Qt.callLater(
                    () => logScroll.contentItem.contentY
                        = Math.max(0, logArea.height - logScroll.height))
            }
        }
    }
}
