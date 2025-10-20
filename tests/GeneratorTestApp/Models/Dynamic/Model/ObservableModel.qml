/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

import QtQuick
import QtQuick.Controls
import Application

Window {
    id: window
    width: 715;
    height: 480;
    visible: true
    title: "Primes! - ObservableCollection<T> model - live updates via item."

    // ViewModel for the ObservableCollection
    ObservablePrimes { id: vm }

    // Button bar for triggering actions on the ViewModel
    Column {
        id: buttonGroups
        spacing: 8
        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.margins: 8

        // INotifyPropertyChanged
        Row {
            spacing: 8
            Text {
                text: "INotifyPropertyChanged:"
                font.bold: true
                verticalAlignment: Text.AlignVCenter
                width: 140
            }
            Button {
                text: "Increment first"; onClicked: vm.incrementFirstN()
            }
            Button {
                text: "Randomize"; onClicked: vm.randomizeSomeNs()
            }
        }

        // ObservableCollection
        Row {
            spacing: 8
            Text {
                text: "ObservableCollection:"
                font.bold: true
                verticalAlignment: Text.AlignVCenter
                width: 140
            }
            Button {
                text: "Add"; onClicked: vm.addNext()
            }
            Button {
                text: "Remove"; onClicked: vm.removeLast()
            }
            Button {
                text: "Replace"; onClicked: vm.replaceFirst()
            }
            Button {
                text: "Move"; onClicked: vm.moveFirstToEnd()
            }
            Button {
                text: "Reset"; onClicked: vm.reset()
            }
        }
    }

    // GridView to display the items
    GridView {
        id: view
        anchors.fill: parent
        anchors.margins: 8
        anchors.topMargin: buttonGroups.height + 16
        cellWidth: width / 10
        cellHeight: height / 10
        model: vm.items  // Bind to the ViewModel's items

        // Animation for item insertion
        add: Transition {
            SequentialAnimation {
                // Highlight the item briefly on insertion
                ScriptAction { script: ViewTransition.item.pulseHighlight() }

                // Fade in and scale animation
                NumberAnimation {
                    target: ViewTransition.item;
                    property: "opacity";
                    from: 0;
                    to: 1;
                    duration: 160
                }
                NumberAnimation {
                    target: ViewTransition.item;
                    property: "scale";
                    from: 0.92;
                    to: 1;
                    duration: 180;
                    easing.type: Easing.OutCubic
                }
            }
        }

        // Animation for item removal
        remove: Transition {
            // Fade out and scale down animation
            ParallelAnimation {
                NumberAnimation {
                    target: ViewTransition.item;
                    property: "opacity";
                    to: 0;
                    duration: 140
                }
                NumberAnimation {
                    target: ViewTransition.item;
                    property: "scale";
                    to: 0.92;
                    duration: 140
                }
            }
        }

        // Animation for items displaced by other operations
        displaced: Transition {
            NumberAnimation {
                properties: "x,y";
                duration: 200;
                easing.type: Easing.OutCubic
            }
        }

        // Animation for item movement (ignored if GridView only uses 'displaced')
        move: Transition {
            SequentialAnimation {
                // Highlight the item briefly on move
                ScriptAction { script: ViewTransition.item.pulseHighlight() }

                // Smooth movement animation
                NumberAnimation {
                    properties: "x,y";
                    duration: 220;
                    easing.type: Easing.OutCubic
                }
            }
        }

        // Delegate for each item in the GridView
        delegate: Rectangle {
            id: card
            required property QtObject item  // Role "item" from the model

            width: GridView.view.cellWidth
            height: GridView.view.cellHeight
            scale: 1
            opacity: 1
            color: "#34c759"
            border.color: Qt.lighter(color, 1.1)
            radius: 6

            // Pulse overlay for visual feedback
            Rectangle {
                id: ink
                anchors.fill: parent
                color: "#ffd60a"
                radius: parent.radius
                opacity: 0

                // Animation for the pulse effect
                SequentialAnimation on opacity {
                    id: pulse
                    NumberAnimation { to: 0.7; duration: 80 }
                    PauseAnimation { duration: 60 }
                    NumberAnimation { to: 0.0; duration: 220 }
                }
            }

            // Function to trigger the pulse animation
            function pulseHighlight() { pulse.restart() }

            // Highlight on item replacement (new QObject at index)
            onItemChanged: pulseHighlight()

            // Highlight on INPC (property change) notifications
            Connections {
                target: item
                function onNChanged() { card.pulseHighlight() }
                function onValueChanged() { card.pulseHighlight() }
            }

            // Bind text to item properties
            Text {
                text: item.value;
                anchors.centerIn: parent;
                anchors.verticalCenterOffset: 4;
                font.pixelSize: parent.width / 4
            }
            Text {
                text: "#" + item.n;
                anchors.top: parent.top;
                anchors.left: parent.left;
                anchors.margins: 2;
                font.pixelSize: parent.width / 6
            }
        }
    }
}
