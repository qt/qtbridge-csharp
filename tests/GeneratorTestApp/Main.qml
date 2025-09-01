/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
import QtQuick
import QtQuick.Controls.Basic
import QtQuick.Controls

Window {
    id: window; width: 320; height: 320; visible: true;
    title: "Primes"

    Program {
        id: program
    }

    ListModel {
        id: menu
        ListElement {
            label: "Delegate calls C# function"
            type: "DelegateCall"
        }
        ListElement {
            label: "Delegate creates C# object"
            type: "DelegateElement"
        }
        ListElement {
            label: "Model from C# list of values"
            type: "ValueListModel"
        }
        ListElement {
            label: "Model from C# list of objects"
            type: "ObjectListModel"
        }
        ListElement {
            label: "Model from C# event"
            type: "ModelFromEvent"
        }
        ListElement {
            label: "Model from QAIM-based C# class"
            type: "ItemModel"
        }
    }

    ListView {
        id: menuView
        anchors.fill: parent
        anchors.margins: 20
        focus: true
        model: menu
        delegate: menuDelegate
        spacing: 5
        clip: true
    }

    Component {
        id: menuDelegate
        Button {
            id: menuOption
            required property string label
            required property string type
            text: label
            font.pixelSize: height / 3
            background: Rectangle {
                implicitWidth: menuView.width
                implicitHeight: menuView.height / (menu.count + 1)
                color: menuOption.down ? "#157efb" : "#53d769"
                border.color: Qt.lighter(color, 1.1)
                border.width: 1
                radius: 5
            }
            onClicked: {
                menuOption.enabled = false
                program.load(menuOption.type)
            }
        }
    }
}
