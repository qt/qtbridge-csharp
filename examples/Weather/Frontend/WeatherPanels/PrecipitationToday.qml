// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui

// Figma "precipitation" card (node 166:3341): title, big mm stat, next-rain
// note.
Rectangle {
    color: Ui.Theme.background_2
    radius: Ui.Spacing.radius_24
    implicitHeight: precipCol.implicitHeight + 32

    ColumnLayout {
        id: precipCol
        anchors.fill: parent
        anchors.margins: 16
        spacing: 16

        Text {
            Layout.fillWidth: true
            text: "Precipitation today"
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.h4_size              // 20 SemiBold
            font.weight:    Ui.Typography.weight_semibold
            color:          Ui.Theme.text_strong
        }

        RowLayout {
            Layout.alignment: Qt.AlignHCenter
            spacing: 4
            Text {
                text: WeatherManager.loading ? "--" : WeatherManager.precipitation.toFixed(0)
                font.family:    Ui.Typography.font_display
                font.pixelSize: Ui.Typography.display_2_size   // 72 Medium
                font.weight:    Ui.Typography.weight_medium
                color:          Ui.Theme.text_strong
            }
            Text {
                Layout.alignment: Qt.AlignBottom
                Layout.bottomMargin: 12
                text: "mm"
                font.family:    Ui.Typography.font_body
                font.pixelSize: Ui.Typography.label_1_size     // 20 Medium
                font.weight:    Ui.Typography.weight_medium
                color:          Ui.Theme.text_strong
            }
        }

        Text {
            Layout.fillWidth: true
            text: WeatherManager.loading ? "" : WeatherManager.nextRainSummary
            wrapMode: Text.WordWrap
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.body_2_size          // 16 Light
            font.weight:    Ui.Typography.weight_light
            color:          Ui.Theme.text_medium
        }
    }
}
