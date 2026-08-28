// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

ExampleWindow {
    screenColumn: 1; screenRow: 1
    title: "Simple Tree"

    SimpleTreeData {
        id: data
    }

    TreeView {
        anchors.fill: parent
        anchors.margins: 8
        clip: true
        model: data
        columnWidthProvider: function(column) {
            return width
        }
        delegate: TreeViewDelegate {
            // TreeViewDelegate's default content item reads model.display, so bind the named role here.
            contentItem: Label {
                text: model.name
            }
        }
    }
}
