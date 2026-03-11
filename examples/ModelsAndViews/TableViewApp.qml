// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ApplicationWindow {
    id: appWin; width: 220; height: 240; visible: true
    title: "Table"

    TableData {
        id: data
    }

    GridLayout {
        anchors.fill: parent
        columns: 2
        rowSpacing: 0
        columnSpacing: 0

        RowLayout {
            Layout.columnSpan: 2
            Button {
                text: "Insert"
                onClicked: {
                    if (data.insertRows(1, 2))
                        view.forceLayout()
                }
            }
            Button {
                text: "Remove"
                onClicked: {
                    if (data.removeRows(1, 2))
                        view.forceLayout()
                }
            }
        }

        HorizontalHeaderView {
            Layout.row: 1
            Layout.column: 1
            Layout.fillWidth: true
            syncView: view
        }

        VerticalHeaderView {
            Layout.fillHeight: true
            implicitWidth: appWin.width / 10
            syncView: view
        }

        TableView {
            id: view
            Layout.fillWidth: true; Layout.fillHeight: true
            model: data
            delegate: TableViewDelegate {
                implicitHeight: 40
                implicitWidth: 9 * appWin.width / 20
                leftPadding: 10; topPadding: 10
            }
        }
    }
}
