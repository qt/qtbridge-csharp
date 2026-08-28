// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ExampleWindow {
    screenColumn: 1
    title: "Table"

    TableData {
        id: data
    }

    function selectRow(row) {
        Qt.callLater(function() {
            view.forceLayout()
            if (view.rows > 0) {
                view.selectionModel.setCurrentIndex(
                    view.index(Math.min(row, view.rows - 1), 0), ItemSelectionModel.Current)
            } else {
                view.selectionModel.clearCurrentIndex()
            }
        })
    }

    GridLayout {
        anchors.fill: parent
        anchors.margins: 8
        columns: 2
        rowSpacing: 8
        columnSpacing: 8

        RowLayout {
            Layout.columnSpan: 2
            Layout.fillWidth: true
            Button {
                Layout.fillWidth: true
                text: "Insert"
                onClicked: {
                    var row = view.selectionModel.currentIndex.row
                    var at = row >= 0 ? Math.min(row + 1, view.rows) : view.rows
                    if (data.insertRows(at, 1))
                        selectRow(at)
                }
            }
            Button {
                Layout.fillWidth: true
                text: "Remove"
                enabled: view.rows > 0 && view.selectionModel.currentIndex.valid
                onClicked: {
                    var row = view.selectionModel.currentIndex.row
                    if (data.removeRows(row, 1))
                        selectRow(row)
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
            implicitWidth: windowWidth() / 10
            syncView: view
        }

        TableView {
            id: view
            Layout.fillWidth: true; Layout.fillHeight: true
            clip: true
            model: data
            selectionModel: ItemSelectionModel { }
            selectionBehavior: TableView.SelectRows
            selectionMode: TableView.SingleSelection
            Component.onCompleted: selectRow(0)
            delegate: TableViewDelegate {
                implicitHeight: 40
                implicitWidth: (windowWidth() - 16 - windowWidth() / 10 - 8) / 2
                leftPadding: 10; topPadding: 10
            }
        }
    }
}
