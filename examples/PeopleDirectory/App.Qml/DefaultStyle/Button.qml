// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls.Basic

Button {
    id: button

    property color contentColor: "navy"
    property color contentColorDown: contentColor
    property color contentColorHover: contentColorDown

    property color backgroundColor: Qt.lighter("lightskyblue", 1.2)
    property color backgroundColorDown: "lightskyblue"
    property color backgroundColorHover: backgroundColorDown

    property color borderColor: contentColor
    property color borderColorDown: backgroundColorDown
    property color borderColorHover: borderColorDown

    property int borderWidth: 0
    property int borderRadius: 4

    contentItem: Text {
        text: button.text
        font: button.font
        color: button.down ? button.contentColorDown
             : button.hovered && button.enabled ? button.contentColorHover
             : button.contentColor
        horizontalAlignment: Text.AlignHCenter
        verticalAlignment: Text.AlignVCenter
        elide: Text.ElideRight
        opacity: enabled ? 1.0 : 0.3
    }

    background: Rectangle {
        implicitWidth: button.background.implicitWidth
        implicitHeight: button.background.implicitHeight
        color: button.down ? button.backgroundColorDown
             : button.hovered && button.enabled ? button.backgroundColorHover
             : button.backgroundColor
        border.color: button.down ? button.borderColorDown
                    : button.hovered && button.enabled ? button.borderColorHover
                    : button.borderColor
        border.width: button.borderWidth
        radius: button.borderRadius
        opacity: enabled ? 1 : 0.3
    }
}
