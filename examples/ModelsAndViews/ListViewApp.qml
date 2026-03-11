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

        TableView {
            id: view
            Layout.fillWidth: true; Layout.fillHeight: true
            model: data
            delegate: TableViewDelegate {
                implicitHeight: 40
                implicitWidth: appWin.width
                leftPadding: 10; topPadding: 10
            }
        }
    }
}
