// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

ApplicationWindow {
    id: mainWindow
    width: 640
    height: 480
    visible: true
    title: "Dynamic Object"

    Window {
        id: viewWindow
        flags: Qt.Tool
        width: 320
        height: 480
        visible: true
        title: "View"
        TreeView {
            id: view
            anchors.fill: parent
            delegate: TreeViewDelegate {
                implicitWidth: 70
                implicitHeight: 40
            }
        }
        Component.onCompleted: Qt.callLater(() => {
            x = mainWindow.x + mainWindow.width + 10
            y = mainWindow.y
        })
    }

    LoadTimeType {
        id: lt
        onPropertyChanged:
            (args) => log.text += "lt: PropertyChanged: " + args.propertyName + "\n"
        onIntPropertyChanged: log.text += "lt: intPropertyChanged\n"
        onIntReadOnlyPropertyChanged: log.text += "lt: intReadOnlyPropertyChanged\n"

        BuildTimeType {
            id: bt
            onPropertyChanged:
                (args) => log.text += "bt: PropertyChanged: " + args.propertyName + "\n"
            onIntPropertyChanged: log.text += "bt: intPropertyChanged\n"
            onIntReadOnlyPropertyChanged: log.text += "bt: intReadOnlyPropertyChanged\n"
        }
    }


    function evaluate(expr) {
        try {
            let result = eval(expr)
            if (result)
                log.text += "\u2714 " + expr + " \u279C " + result + "\n"
            else
                log.text += "\u2718 " + expr + " \u279C " + result + "\n"
        } catch (error) {
            log.text += "\u26A0 " + expr + "\n"
            log.text += "\u26A0 " + error + "\n"
        }
    }

    ScrollView {
        id: scroll
        anchors.fill: parent
        TextArea {
            id: log
            readOnly: true
            font.pointSize: 12
            onTextChanged: Qt.callLater(() => scroll.contentItem.contentY = height - scroll.height)
        }
    }

    menuBar: MenuBar {
        TestObject {
            title: "BuildTimeType { id: bt }"
            object: "bt"
        }
        TestObject {
            title: "LoadTimeType { id: lt }"
            object: "lt"
        }
    }

    header: Row {
        leftPadding: mainWindow.width - btProp.width - ltProp.width
        TextField {
            id: btProp
            topPadding: 6
            leftPadding: 10
            readOnly: true
            text: "bt.intProperty: " + bt.intProperty
        }
        TextField {
            id: ltProp
            topPadding: 6
            leftPadding: 10
            readOnly: true
            text: "lt.intProperty: " + lt.intProperty
        }
    }

    footer: TextField {
        focus: true
        placeholderText: "Enter expression..."
        implicitHeight: 30
        topPadding: 6
        leftPadding: 10
        onAccepted: evaluate(text)
    }
}
