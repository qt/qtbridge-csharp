// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui

// Figma "SidePanelItem" (node 166:3201): location row, rounded sky photo
// filling the available space, then the big temperature + feels-like.
Item {
    id: root

    ColumnLayout {
        anchors.fill: parent
        spacing: 8

        // Location
        RowLayout {
            Layout.fillWidth: true
            spacing: 8
            Ui.Icon {
                size: 16
                source: Qt.resolvedUrl("../assets/icons/location_pin.svg")
                color: Ui.Theme.text_medium
            }
            Text {
                Layout.fillWidth: true
                text: WeatherManager.locationName
                elide: Text.ElideRight
                font.family:    Ui.Typography.font_body
                font.pixelSize: Ui.Typography.body_2_size      // 16 Light
                font.weight:    Ui.Typography.weight_light
                color:          Ui.Theme.text_medium
            }
        }

        // Weather illustration (Figma "WeatherImages"), theme-aware
        WeatherImage {
            Layout.fillWidth: true
            Layout.fillHeight: true
            weatherCode: WeatherManager.loading ? 2 : WeatherManager.weatherCode
        }

        Text {
            Layout.fillWidth: true
            Layout.topMargin: 8
            horizontalAlignment: Text.AlignHCenter
            text: WeatherManager.loading
                  ? "--°C" : WeatherManager.temperature.toFixed(0) + "°C"
            font.family:    Ui.Typography.font_display
            font.pixelSize: Ui.Typography.display_2_size       // 72 Medium
            font.weight:    Ui.Typography.weight_medium
            color:          Ui.Theme.text_strong
        }

        Text {
            Layout.fillWidth: true
            Layout.topMargin: 8
            horizontalAlignment: Text.AlignHCenter
            text: WeatherManager.loading
                  ? "" : "Feels like: " + WeatherManager.feelsLike.toFixed(0) + "°C"
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.body_1_size          // 20 Light
            font.weight:    Ui.Typography.weight_light
            color:          Ui.Theme.text_medium
        }
    }
}
