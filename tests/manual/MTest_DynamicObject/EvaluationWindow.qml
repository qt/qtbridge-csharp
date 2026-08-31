// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

Window {
    property string resultText: ""
    signal evaluationRequested(string expression)

    width: 480
    height: 320
    visible: false
    title: "Evaluation"
    color: "#f3f4f6"

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 20
        spacing: 10

        Label {
            Layout.fillWidth: true
            text: "Evaluate JavaScript in this file's scope. Use bt and lt, for example: "
                + "bt.item(0) or bt.intProperty = 99."
            wrapMode: Text.WordWrap
            color: "#5f6368"
        }
        RowLayout {
            Layout.fillWidth: true
            spacing: 10

            TextField {
                id: expressionInput
                Layout.fillWidth: true
                placeholderText: "bt.intProperty = 99"
                onAccepted: evaluationRequested(text)
                Keys.onEscapePressed: text = ""
            }
            Button {
                text: "Evaluate"
                leftPadding: 10
                rightPadding: 10
                onClicked: evaluationRequested(expressionInput.text)
            }
        }

        Label {
            text: "Result"
            font.bold: true
        }
        ScrollView {
            Layout.fillWidth: true
            Layout.fillHeight: true
            clip: true

            TextArea {
                readOnly: true
                selectByMouse: true
                text: resultText
                wrapMode: Text.WrapAnywhere
                font.family: "Consolas"
                font.pixelSize: 12
                color: "#1c1e21"
            }
        }
    }
}
