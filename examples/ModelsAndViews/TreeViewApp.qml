// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ApplicationWindow {
    id: appWin; width: 220; height: 240; visible: true
    Component.onCompleted: x += 1.5 * width
    title: "Tree"

    TreeData {
        id: data
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        RowLayout {
            Layout.columnSpan: 2
            Button {
                text: "Insert"
                onClicked: {
                    var exp = view.isExpanded(1)
                    if (exp)
                        view.collapse(1)
                    if (data.insertRows(1, 2, data.index(1, 0)))
                        view.forceLayout()
                    if (exp)
                        view.expand(1)
                }
            }
            Button {
                text: "Remove"
                onClicked: {
                    var exp = view.isExpanded(1)
                    if (exp)
                        view.collapse(1)
                    if (data.removeRows(1, 2, data.index(1, 0)))
                        view.forceLayout()
                    if (exp)
                        view.expand(1)
                }
            }
        }

        HorizontalHeaderView {
            id: horizontalHeader
            Layout.fillWidth: true
            syncView: view
        }

        TreeView {
            id: view
            Layout.fillWidth: true; Layout.fillHeight: true
            model: data
            delegate: TreeViewDelegate {
                implicitHeight: 40
                implicitWidth: appWin.width / 2
            }
            editTriggers: TableView.DoubleTapped
        }
    }
}
