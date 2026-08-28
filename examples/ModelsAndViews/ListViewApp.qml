// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ExampleWindow {
    title: "List"

    ListData {
        id: data
    }

    function selectRow(row) {
        Qt.callLater(function() {
            view.forceLayout()
            if (view.rows > 0) {
                view.selectionModel.setCurrentIndex(view.index(Math.min(row, view.rows - 1), 0),
                    ItemSelectionModel.Current)
            } else {
                view.selectionModel.clearCurrentIndex()
            }
        })
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 8
        spacing: 8

        RowLayout {
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

        Item {
            Layout.fillWidth: true; Layout.fillHeight: true

            Rectangle {
                anchors.fill: parent
                color: "white"
            }

            TableView {
                id: view
                anchors.fill: parent
                clip: true
                model: data
                selectionModel: ItemSelectionModel { }
                selectionBehavior: TableView.SelectRows
                selectionMode: TableView.SingleSelection
                Component.onCompleted: selectRow(0)
                delegate: TableViewDelegate {
                    implicitHeight: 40
                    implicitWidth: windowWidth() - 16
                    leftPadding: 10; topPadding: 10
                }
            }
        }
    }
}
