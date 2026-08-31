// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

// Runs are delegated to the owner via runRequested, since bt/lt live there.
ColumnLayout {
    id: root
    spacing: 8

    required property var fixturesModel

    readonly property int statusColumnWidth: 230

    signal runRequested(int index, string path)
    signal logRequested()

    function statusBackground(status) {
        if (status === "PASS")
            return "#e3f5e8"
        if (status === "FAIL")
            return "#fdeaea"
        return "#e9eaec"
    }

    function statusColor(status) {
        if (status === "PASS")
            return "#1e7d34"
        if (status === "FAIL")
            return "#c62828"
        return "#5f6368"
    }

    function rowColor(index) {
        return index % 2 === 0 ? "#ffffff" : "#f6f7f8"
    }

    function cellColor(index, status) {
        return status === "Not run" ? rowColor(index) : statusBackground(status)
    }

    // Align the filter field's width to the first row's content width.
    readonly property real firstRowWidth: infoRepeater.count > 0
        ? infoRepeater.itemAt(0).implicitWidth
        : 0

    // A row with all cells invisible collapses to zero height in GridLayout.
    function matchesFilter(index) {
        const filter = filterField.text.trim().toLowerCase()
        if (filter === "")
            return true
        const entry = root.fixturesModel.get(index)
        return entry.name.toLowerCase().includes(filter)
            || entry.expression.toLowerCase().includes(filter)
    }

    function scrollToFirstFailure() {
        for (let index = 0; index < root.fixturesModel.count; ++index) {
            const entry = root.fixturesModel.get(index)
            if (entry.buildStatus === "FAIL" || entry.loadStatus === "FAIL") {
                const item = infoRepeater.itemAt(index)
                if (item)
                    fixturesScroll.contentItem.contentY = Math.max(0, item.y)
                return
            }
        }
    }

    // Fixed header, keep out of the ScrollView so it doesn't move with the rows.
    RowLayout {
        Layout.fillWidth: true
        spacing: 12

        Label {
            text: "Check and expression"
            font.bold: true
        }
        TextField {
            id: filterField
            Layout.fillWidth: true
            Layout.maximumWidth: root.firstRowWidth > 0
                ? root.firstRowWidth : Number.POSITIVE_INFINITY
            placeholderText: "Filter fixtures…"
            Keys.onEscapePressed: text = ""
        }
        // Grab leftover space once filterField hits its max width.
        Item {
            Layout.fillWidth: true
        }
        Label {
            text: "Build-time (bt)"
            font.bold: true
            Layout.preferredWidth: root.statusColumnWidth
        }
        Label {
            text: "Load-time (lt)"
            font.bold: true
            Layout.preferredWidth: root.statusColumnWidth
        }
    }

    Rectangle {
        Layout.fillWidth: true
        Layout.preferredHeight: 1
        color: "#d7d9dc"
    }

    ScrollView {
        id: fixturesScroll
        Layout.fillWidth: true
        Layout.fillHeight: true
        clip: true

        GridLayout {
            id: fixturesGrid
            width: fixturesScroll.availableWidth
            columns: 3
            columnSpacing: 12
            rowSpacing: 8

            Repeater {
                id: infoRepeater
                model: root.fixturesModel
                delegate: Rectangle {
                    id: infoCell
                    required property int index
                    required property string name
                    required property string display

                    visible: root.matchesFilter(index)
                    Layout.row: index
                    Layout.column: 0
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    // Rectangle doesn't auto-size to children like ColumnLayout does.
                    implicitWidth: contentLayout.implicitWidth + 22
                    implicitHeight: contentLayout.implicitHeight + 28
                    radius: 8
                    color: root.rowColor(index)
                    border.color: "#e3e5e8"
                    border.width: 1

                    ColumnLayout {
                        id: contentLayout
                        anchors.fill: parent
                        anchors.topMargin: 14
                        anchors.bottomMargin: 14
                        anchors.leftMargin: 14
                        anchors.rightMargin: 8
                        spacing: 4

                        Label {
                            text: infoCell.name
                            font.bold: true
                            font.pixelSize: 15
                        }
                        Label {
                            Layout.fillWidth: true
                            text: infoCell.display
                            wrapMode: Text.WrapAnywhere
                            font.family: "Consolas"
                            font.pixelSize: 12
                            color: "#4a4f57"
                        }
                    }
                }
            }

            Repeater {
                model: root.fixturesModel
                delegate: Rectangle {
                    id: buildCell
                    required property int index
                    required property string buildStatus
                    required property string buildDetails

                    visible: root.matchesFilter(index)
                    Layout.row: index
                    Layout.column: 1
                    Layout.preferredWidth: root.statusColumnWidth
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    implicitHeight: contentLayout.implicitHeight + 28
                    radius: 8
                    color: root.cellColor(index, buildStatus)
                    border.color: "#e3e5e8"
                    border.width: 1

                    RowLayout {
                        id: contentLayout
                        anchors.centerIn: parent
                        spacing: 10

                        Button {
                            text: "Run"
                            leftPadding: 10
                            rightPadding: 10
                            onClicked: root.runRequested(buildCell.index, "build")
                        }
                        Button {
                            leftPadding: 10
                            rightPadding: 10
                            visible: buildCell.buildStatus === "FAIL"
                            text: "View log"
                            font.pixelSize: 11
                            onClicked: root.logRequested()
                        }
                        Label {
                            text: buildCell.buildStatus
                            font.bold: true
                            font.pixelSize: 11
                            color: root.statusColor(buildCell.buildStatus)
                        }
                    }
                }
            }

            Repeater {
                model: root.fixturesModel
                delegate: Rectangle {
                    id: loadCell
                    required property int index
                    required property string loadStatus
                    required property string loadDetails

                    visible: root.matchesFilter(index)
                    Layout.row: index
                    Layout.column: 2
                    Layout.preferredWidth: root.statusColumnWidth
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    implicitHeight: contentLayout.implicitHeight + 28
                    radius: 8
                    color: root.cellColor(index, loadStatus)
                    border.color: "#e3e5e8"
                    border.width: 1

                    RowLayout {
                        id: contentLayout
                        anchors.centerIn: parent
                        spacing: 10

                        Button {
                            text: "Run"
                            leftPadding: 10
                            rightPadding: 10
                            onClicked: root.runRequested(loadCell.index, "load")
                        }
                        Button {
                            leftPadding: 10
                            rightPadding: 10
                            visible: loadCell.loadStatus === "FAIL"
                            text: "View log"
                            onClicked: root.logRequested()
                        }
                        Label {
                            text: loadCell.loadStatus
                            font.bold: true
                            font.pixelSize: 11
                            color: root.statusColor(loadCell.loadStatus)
                        }
                    }
                }
            }
        }
    }

    Label {
        Layout.fillWidth: true
        text: "Each row runs the same operation against the source-generated BuildTimeType "
            + "(bt) and the metadata-driven LoadTimeType (lt). The check code uses target "
            + "as a placeholder for the selected object."
        wrapMode: Text.WordWrap
        color: "#5f6368"
    }
}

