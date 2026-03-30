// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ApplicationWindow {
    id: appWin; width: 220; height: 240; visible: true
    Component.onCompleted: x -= 1.5 * width
    title: "List"

    ListData {
        id: data
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        RowLayout {
            Button {
                text: "Insert"
                onClicked: {
                    var row = view.selectionModel.currentIndex.row
                    var at = row >= 0 ? Math.min(row + 1, view.rows) : view.rows
                    if (!data.insertRows(at, 1))
                        return;

                    Qt.callLater(function() {
                        view.forceLayout()
                        if (view.rows > at) {
                            view.selectionModel.setCurrentIndex(view.index(at, 0),
                                ItemSelectionModel.Current)
                        }
                    })
                }
            }
            Button {
                text: "Remove"
                enabled: view.rows > 0 && view.selectionModel.currentIndex.valid
                onClicked: {
                    var row = view.selectionModel.currentIndex.row
                    if (!data.removeRows(row, 1))
                        return;

                    Qt.callLater(function() {
                        view.forceLayout()
                        if (view.rows > 0) {
                            var nextRow = Math.min(row, view.rows - 1)
                            view.selectionModel.setCurrentIndex(view.index(nextRow, 0),
                                ItemSelectionModel.Current)
                        } else {
                            view.selectionModel.clearCurrentIndex()
                        }
                    })
                }
            }
        }

        TableView {
            id: view
            Layout.fillWidth: true; Layout.fillHeight: true
            model: data
            selectionModel: ItemSelectionModel { }
            selectionBehavior: TableView.SelectRows
            selectionMode: TableView.SingleSelection
            Component.onCompleted: {
                if (rows > 0)
                    selectionModel.setCurrentIndex(index(0, 0), ItemSelectionModel.Current)
            }
            delegate: TableViewDelegate {
                implicitHeight: 40
                implicitWidth: appWin.width
                leftPadding: 10; topPadding: 10
            }
        }
    }
}
