// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Dialogs
import QtQuick.Layouts

ApplicationWindow {
    id: mainWindow
    width: 800
    height: 600
    visible: true
    title: "Spreadsheet Sandbox" + (Spreadsheet.fileName ? ": " + Spreadsheet.fileName : "")

    MessageDialog {
        id: msgDialog
        buttons: MessageDialog.Ok
        function show(msg) {
            text = msg
            open()
        }
    }

    FileDialog {
        id: fileDialog
        nameFilters: ["Excel files (*.xlsx)", "All files (*)"]
        onAccepted: {
            if (Spreadsheet.openFile(selectedFile)) {
                view.selectTopLeft()
            } else {
                msgDialog.show("Error opening file")
            }
        }
    }

    menuBar: MenuBar {
        Menu {
            title: "&File"
            MenuItem {
                text: "&Open..."
                onTriggered: fileDialog.open()
            }
            MenuSeparator { }
            MenuItem {
                text: "E&xit"
                onTriggered: mainWindow.close()
            }
        }
    }

    GridLayout {
        anchors.fill: mainWindow.contentItem
        columns: 2
        rowSpacing: 0
        columnSpacing: 0

        TextField {
            id: formulaEdit
            Layout.columnSpan: 2
            Layout.fillWidth: true;
            implicitHeight: 30
            leftPadding: 10
            topPadding: 6
            onAccepted: {
                view.setFormula(text)
                view.focus = true
            }
            Keys.onEscapePressed: (keyEvent) => {
                keyEvent.accepted = true
                view.resetFormula()
                view.focus = true
            }
        }

        HorizontalHeaderView {
            id: columnHeaders
            Layout.row: 1
            Layout.column: 1
            Layout.fillWidth: true
            syncView: view
            property int menuColumn: -1
            ContextMenu.onRequested: position
                => columnHeaders.menuColumn = cellAtPosition(position, true).x
            ContextMenu.menu: Menu {
                enabled: columnHeaders.menuColumn != -1
                MenuItem {
                    text: "Insert"
                    onTriggered: {
                        if (Spreadsheet.insertColumns(columnHeaders.menuColumn, 1))
                            view.forceLayout()
                    }
                }
                MenuItem {
                    text: "Delete"
                    onTriggered: {
                        if (Spreadsheet.removeColumns(columnHeaders.menuColumn, 1))
                            view.forceLayout()
                    }
                }
            }
        }

        VerticalHeaderView {
            id: rowHeaders
            Layout.fillHeight: true
            syncView: view
            property int menuRow: -1
            ContextMenu.onRequested: position
                => rowHeaders.menuRow = cellAtPosition(position, true).y
            ContextMenu.menu: Menu {
                enabled: rowHeaders.menuRow != -1
                MenuItem {
                    text: "Insert"
                    onTriggered: {
                        if (Spreadsheet.insertRows(rowHeaders.menuRow, 1))
                            view.forceLayout()
                    }
                }
                MenuItem {
                    text: "Delete"
                    onTriggered: {
                        if (Spreadsheet.removeRows(rowHeaders.menuRow, 1))
                            view.forceLayout()
                    }
                }
            }
        }

        TableView {
            id: view
            focus: true
            Layout.fillWidth: true
            Layout.fillHeight: true
            model: Spreadsheet
            delegate: TableViewDelegate {
                implicitHeight: 40
                implicitWidth: 90
                leftPadding: 10
                topPadding: 12
                Rectangle {
                    anchors.fill: parent
                    color: "transparent"
                    border.color: "#35000000"
                    border.width: 1
                }
                onEditingChanged: {
                    if (current)
                        formulaEdit.text = model.edit
                }
                onCurrentChanged: {
                    if (current)
                        formulaEdit.text = model.edit
                }
            }
            selectionModel: ItemSelectionModel { }
            editTriggers: TableView.AnyKeyPressed | TableView.DoubleTapped

            Component.onCompleted: selectTopLeft()

            Keys.onDeletePressed: (keyEvent) => {
                keyEvent.accepted = true
                Spreadsheet.clearItemData(currentRow, currentColumn)
                resetFormula()
            }

            function selectTopLeft() {
                selectionModel.setCurrentIndex(Spreadsheet.index(0,0), 0)
                focus = true
            }

            function setFormula(value) {
                model.setData(selectionModel.currentIndex, value, Qt.EditRole)
            }

            function resetFormula() {
                formulaEdit.text = model.data(selectionModel.currentIndex, Qt.EditRole)
            }
        }
    }
}
