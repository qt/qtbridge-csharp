// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

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
            uri: "Models.Static.Range"
            type: "DelegateCall"
        }
        ListElement {
            label: "Delegate creates C# object"
            uri: "Models.Static.Range"
            type: "DelegateElement"
        }
        ListElement {
            label: "Model from C# list of values"
            uri: "Models.Static.Function"
            type: "ValueListModel"
        }
        ListElement {
            label: "Model from C# list of objects"
            uri: "Models.Static.Function"
            type: "ObjectListModel"
        }
        ListElement {
            label: "Model from C# event"
            uri: "Models.Dynamic.Event"
            type: "ModelFromEvent"
        }
        ListElement {
            label: "Model from QAIM-based C# class"
            uri: "Models.Dynamic.Model"
            type: "ItemModel"
        }
        ListElement {
            label: "ObservableCollection<T> live model"
            uri: "Models.Dynamic.Model"
            type: "ObservableModel"
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
            required property string uri
            required property string type
            width: menuView.width
            height: menuView.height / (menu.count + 1)
            text: label
            font.pixelSize: height / 3
            background: Rectangle {
                width: menuOption.width
                height: menuOption.height
                color: menuOption.down ? "#157efb" : "#53d769"
                border.color: Qt.lighter(color, 1.1)
                border.width: 1
                radius: 5
            }
            onClicked: {
                menuOption.enabled = false
                program.load(menuOption.uri, menuOption.type)
            }
        }
    }
}
