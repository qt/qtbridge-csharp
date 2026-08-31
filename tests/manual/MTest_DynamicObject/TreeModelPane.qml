// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ColumnLayout {
    id: root

    required property string label
    required property var model

    // Loader doesn't stretch loaded Layout items to its own size by default.
    anchors.fill: parent
    spacing: 6

    Label {
        text: label
        font.bold: true
    }

    HorizontalHeaderView {
        id: header
        Layout.fillWidth: true
        syncView: treeView
        clip: true
    }

    TreeView {
        id: treeView
        Layout.fillWidth: true
        Layout.fillHeight: true
        clip: true
        model: root.model
        columnWidthProvider: function (column) {
            return column === 0 ? width * 0.6 : width * 0.4
        }
        delegate: TreeViewDelegate {
            implicitHeight: 32
        }
    }
}
