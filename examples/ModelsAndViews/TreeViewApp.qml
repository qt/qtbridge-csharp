// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ExampleWindow {
    screenRow: 1
    title: "Tree"

    TreeData {
        id: data
    }

    // Preserves the expand state of the node addressed by `index` around a mutation of
    // its children, since forceLayout() can otherwise leave an expanded node collapsed.
    function withPreservedExpansion(index, mutate) {
        var row = view.rowAtIndex(index)
        var exp = row >= 0 && view.isExpanded(row)
        if (exp)
            view.collapse(row)
        if (mutate())
            view.forceLayout()
        if (exp)
            view.expand(row)
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 8
        spacing: 8

        RowLayout {
            Layout.columnSpan: 2
            Layout.fillWidth: true
            SpinBox {
                id: rowCount
                from: 1; to: 5
                value: 1
            }
            Button {
                Layout.fillWidth: true
                text: "Insert"
                // Only a top-level band node (not a member) may get new members inserted,
                // keeping the tree flat at two levels.
                enabled: {
                    var index = view.selectionModel.currentIndex
                    return index.valid && !data.parent(index).valid
                }
                onClicked: {
                    var index = view.selectionModel.currentIndex
                    withPreservedExpansion(index, function() {
                        return data.insertRows(0, rowCount.value, index)
                    })
                }
            }
            Button {
                Layout.fillWidth: true
                text: "Remove"
                // TreeData only allows removing rows that have a valid parent, i.e. not the
                // top-level band nodes.
                enabled: {
                    var index = view.selectionModel.currentIndex
                    return index.valid && data.parent(index).valid
                }
                onClicked: {
                    var index = view.selectionModel.currentIndex
                    var parent = data.parent(index)
                    withPreservedExpansion(parent, function() {
                        return data.removeRows(index.row, rowCount.value, parent)
                    })
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
            clip: true
            model: data
            selectionModel: ItemSelectionModel { }
            selectionBehavior: TableView.SelectRows
            selectionMode: TableView.SingleSelection
            delegate: TreeViewDelegate {
                implicitHeight: 40
                implicitWidth: (windowWidth() - 16) / 2
            }
            editTriggers: TableView.DoubleTapped
            Component.onCompleted: selectionModel.setCurrentIndex(data.index(0, 0),
                ItemSelectionModel.Current)
        }
    }
}
