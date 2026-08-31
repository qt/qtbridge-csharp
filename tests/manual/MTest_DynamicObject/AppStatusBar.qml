// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ToolBar {
    required property var buildObject
    required property var loadObject
    required property string fixturesFileName
    required property string fixturesLoadError

    RowLayout {
        anchors.fill: parent
        anchors.leftMargin: 12
        anchors.rightMargin: 12
        spacing: 16

        Label {
            text: "bt.intProperty: " + buildObject.intProperty
            font.family: "Consolas"
            font.pixelSize: 12
            color: "#1c1e21"
        }
        Label {
            text: "lt.intProperty: " + loadObject.intProperty
            font.family: "Consolas"
            font.pixelSize: 12
            color: "#1c1e21"
        }
        Item {
            Layout.fillWidth: true
        }
        Label {
            elide: Text.ElideMiddle
            text: fixturesLoadError !== ""
                ? "Failed to load " + fixturesFileName + ": " + fixturesLoadError
                : "Fixtures loaded from " + fixturesFileName + "."
            color: fixturesLoadError !== "" ? "#c62828" : "#5f6368"
            font.italic: true
            font.pixelSize: 12
        }
    }
}
