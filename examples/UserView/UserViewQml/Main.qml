/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

import QtQuick
import QtQuick.Controls
import QtQuick.Effects
import QtQuick.Layouts

ApplicationWindow {
    id: mainWindow
    visible: true
    width: 480
    height: Screen.desktopAvailableHeight

    UserViewApp { id: app }
    property UserListModel userList: app.users
    title: "Users: " + userList.count.toString()

    ListView {
        anchors.fill: parent
        model: userList
        delegate: RowLayout {
            id: user
            required property string fullName
            required property string firstName
            required property string lastName
            required property string email
            required property string thumbnail
            required property string picture
            required property date birthDate
            required property int age
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
                    text: user.birthDate.toISOString().split("T")[0] + " (" + user.age.toString() + ")"
                    font.pixelSize: 10
                }
                Text {
                    Layout.alignment: Qt.AlignLeft
                    text: user.email
                    font.pixelSize: 14
                }
            }
        }
        section.property: "lastName"
        section.criteria: ViewSection.FirstCharacter
        section.delegate: Rectangle {
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

    Button {
        id: buttonAdd
        opacity: 0.85; x: parent.width - 150; y: 10; width: 140; height: 40
        text: "Add"
        onClicked: {
            app.add()
        }
    }

    Button {
        id: buttonRemove
        opacity: 0.85; x: parent.width - 150; y: 60; width: 140; height: 40
        text: "Remove"
        onClicked: {
            app.remove()
        }
    }

    Connections {
        target: app

        function onUserAdded(args) {
            popup.name = args.user.name.full
            popup.picture = args.user.picture.large
            popup.description = "Added on "
                + args.timestamp.toISOString().replace("T", " ").split(".")[0]
            if (!popup.visible)
                popup.open()
            popupClose.restart();
        }

        function onUserRemoved(args) {
            popup.name = args.user.name.full
            popup.picture = args.user.picture.large
            popup.description = "Removed on "
                + args.timestamp.toISOString().replace("T", " ").split(".")[0]
            if (!popup.visible)
                popup.open()
            popupClose.restart();
        }
    }

    Popup {
        id: popup
        property string name
        property string picture
        property string description
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
            Text {
                Layout.alignment: Qt.AlignCenter
                text: popup.description
                font.pixelSize: 12
            }
        }
    }

    Timer {
        id: popupClose
        interval: 2500; running: false; repeat: false
        onTriggered: popup.close()
    }
}
