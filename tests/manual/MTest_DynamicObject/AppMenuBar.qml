// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

MenuBar {
    required property var fileOpenAction
    required property var applicationExitAction
    required property var viewModelAction
    required property var viewEvaluationAction
    required property var viewLogAction
    required property var fixturesRunAllAction
    required property var fixturesResetAction

    component ShortcutMenuItem: MenuItem {
        id: menuItem
        implicitWidth: 240
        implicitHeight: 30

        Shortcut {
            id: shortcutText
            enabled: false
            sequence: menuItem.action ? menuItem.action.shortcut : ""
        }

        contentItem: RowLayout {
            anchors.fill: parent
            anchors.leftMargin: 12
            anchors.rightMargin: 12
            spacing: 16

            Label {
                Layout.fillWidth: true
                text: menuItem.text
                color: menuItem.enabled ? "#333333" : "#888888"
            }
            Label {
                text: shortcutText.nativeText
                color: "#888888"
                font.pixelSize: 11
            }
        }

        background: Rectangle {
            color: menuItem.highlighted ? "#e5e5e5" : "transparent"
        }
    }

    Menu {
        title: "&File"
        ShortcutMenuItem { action: fileOpenAction }
        MenuSeparator {}
        ShortcutMenuItem { action: applicationExitAction }
    }

    Menu {
        title: "&View"
        ShortcutMenuItem { action: viewEvaluationAction }
        ShortcutMenuItem { action: viewModelAction }
        MenuSeparator {}
        ShortcutMenuItem { action: viewLogAction }
    }

    Menu {
        title: "&Run"
        ShortcutMenuItem { action: fixturesRunAllAction }
        ShortcutMenuItem { action: fixturesResetAction }
    }
}
