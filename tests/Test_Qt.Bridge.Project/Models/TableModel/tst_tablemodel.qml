// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtQuick.Controls
import QtTest
import Application

TableView {
    id: view
    width: 100
    height: 100
    visible: true

    property bool viewCompleted: false
    Component.onCompleted: viewCompleted = true

    model: TestTable {
        id: data
    }

    TestCase {
        id: test
        name: "tst_tablemodel"
        when: viewCompleted

        function test_insert_columns() {
            verify(data.insertColumns(0, 1))
            view.forceLayout()
        }

        function test_insert_rows() {
            verify(data.insertRows(0, 1))
            view.forceLayout()
        }
    }

    delegate: TableViewDelegate {
        Connections {
            target: contentItem
            function onTextChanged() {
                var msg = "TableViewDelegate: [%1, %2] = %3"
                console.log(msg.arg(row).arg(column).arg(contentItem.text))
                test.compare(parseInt(contentItem.text), column - row)
            }
        }
    }
}
