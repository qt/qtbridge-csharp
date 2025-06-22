/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

import qmlapp

import QtQuick
import QtQuick.Controls
import QtQuick.Effects
import QtQuick.Layouts

ApplicationWindow {
    id: mainWindow
    visible: true
    width: 480
    height: Screen.desktopAvailableHeight
    title: "Users"

    ListView {
        anchors.fill: parent
        model: QmlApp.users
        section.property: "lastName"
        section.criteria: ViewSection.FirstCharacter
        section.delegate: sectionHeading
        delegate: RowLayout {
            id: user
            required property string fullName
            required property string firstName
            required property string lastName
            required property string email
            required property string thumbnail
            required property string picture
            Image {
                id: userPicture
                source: user.thumbnail
                Layout.rowSpan: 2; Layout.column: 0; Layout.margins: 6;
                Layout.preferredWidth: 64; Layout.preferredHeight: 64
                layer.enabled: true;
                layer.effect: MultiEffect { maskEnabled: true; maskSource: mask }
            }
            ColumnLayout {
                Text {
                    Layout.alignment: Qt.AlignLeft
                    text: user.firstName + " " + user.lastName
                    font.bold: true; font.pixelSize: 20
                }
                Text {
                    Layout.alignment: Qt.AlignLeft
                    text: user.email
                    font.pixelSize: 14
                }
            }
        }
        add: Transition {
            NumberAnimation { properties: "x,y"; from: 100; duration: 500 }
        }
        addDisplaced: Transition {
            NumberAnimation { properties: "x,y"; duration: 500 }
        }
        remove: Transition {
            ParallelAnimation {
                NumberAnimation { property: "opacity"; to: 0; duration: 500 }
                NumberAnimation { properties: "x,y"; to: 100; duration: 500 }
            }
        }
        removeDisplaced: Transition {
            NumberAnimation { properties: "x,y"; duration: 500 }
        }
    }

    Item {
        id: mask
        width: 64; height: 64; layer.enabled: true; visible: false
        Rectangle { width: 64; height: 64; radius: 16; color: "black" }
    }

    Component {
        id: sectionHeading
        Rectangle {
            width: ListView.view.width; height: initial.height + 10
            color: "transparent"
            required property string section
            Text {
                id: initial; text: parent.section
                color: "#00414A"; anchors.bottom: parent.bottom; anchors.bottomMargin: 2
                font.bold: true; font.pixelSize: 20; font.capitalization: Font.AllUppercase
            }
            Rectangle {
                width: parent.width; height: 2
                anchors.bottom: parent.bottom
                color: "#2CDE85"
            }
        }
    }

    Button {
        opacity: 0.85; x: parent.width - 150; y: 10; width: 90; height: 40
        text: "Add"
        onClicked: QmlApp.add()
    }

    SpinBox {
        opacity: 0.85; x: parent.width - 55; y: 10; width: 45; height: 40
        value: QmlApp.amountToAdd
        onValueModified: QmlApp.amountToAdd = value
    }

    Button {
        opacity: 0.85; x: parent.width - 150; y: 60; width: 140; height: 40
        text: "Remove"
        onClicked: QmlApp.remove()
    }

    Connections {
        target: QmlApp

        function onUserAdded(name, picture, timestamp) {
            popup.name = name
            popup.picture = picture
            if (!popup.visible)
                popup.open()
            popupClose.restart();
        }

        function onUserRemoved(name, picture, timestamp) {
            popup.name = name
            popup.picture = picture
            if (!popup.visible)
                popup.open()
            popupClose.restart();
        }
    }

    Popup {
        id: popup
        property string name
        property string picture
        popupType: Popup.Item
        anchors.centerIn: parent
        modal: false; focus: true
        closePolicy: Popup.CloseOnEscape
        enter: Transition {
            NumberAnimation { property: "opacity"; from: 0.0; to: 1.0 }
        }
        exit: Transition {
            NumberAnimation { property: "opacity"; from: 1.0; to: 0.0 }
        }
        ColumnLayout {
            Image {
                source: popup.picture
                Layout.margins: 6
                Layout.preferredWidth: 256
                Layout.preferredHeight: 256
            }
            Text {
                Layout.alignment: Qt.AlignCenter
                text: popup.name
                font.bold: true; font.pixelSize: 20
            }
        }
    }

    Timer {
        id: popupClose
        interval: 2500; running: false; repeat: false
        onTriggered: popup.close()
    }
}
