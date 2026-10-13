// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui
import "WeatherPanels"

// Weather window — Figma (node 159:5929): H1 header, current conditions with
// sky photo (left) above the hourly forecast, and a right column with Wind,
// Precipitation and Daily forecast cards.
Item {
    id: root

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        Text {
            Layout.fillWidth: true
            Layout.leftMargin: 32; Layout.rightMargin: 32
            Layout.topMargin: 32; Layout.bottomMargin: 16
            text: "Weather"
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.h1_size          // 48 SemiBold
            font.weight:    Ui.Typography.weight_semibold
            color:          Ui.Theme.text_strong
        }

        Rectangle {
            Layout.fillWidth: true
            Layout.leftMargin: 32; Layout.rightMargin: 32; Layout.bottomMargin: 16
            implicitHeight: errorText.implicitHeight + 16
            radius: Ui.Spacing.radius_8
            color: Ui.Theme.status_warning_subtle
            visible: WeatherManager.errorMessage.length > 0

            Text {
                id: errorText
                anchors.fill: parent
                anchors.margins: 8
                text: WeatherManager.errorMessage
                wrapMode: Text.WordWrap
                font.family: Ui.Typography.font_body
                font.pixelSize: Ui.Typography.body_2_size
                font.weight: Ui.Typography.weight_medium
                color: Ui.Theme.status_warning
            }
        }

        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            Layout.leftMargin: 32; Layout.rightMargin: 32; Layout.bottomMargin: 32
            spacing: 24

            // Left: location + photo + current temperature, hourly forecast
            ColumnLayout {
                Layout.fillWidth: true
                Layout.fillHeight: true
                spacing: 16

                TemperatureNow {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                }
                HourlyForecastToday {
                    Layout.fillWidth: true
                }
            }

            // Right: secondary cards (Figma width 309/896 of the content row).
            // fillWidth must be explicit — nested layouts default to filling,
            // which would starve the left column.
            ColumnLayout {
                Layout.fillWidth: false
                Layout.fillHeight: true
                Layout.preferredWidth: Math.max(250, Math.round(root.width * 0.345))
                spacing: 16

                Wind { Layout.fillWidth: true }
                PrecipitationToday { Layout.fillWidth: true }
                DailyForecast { Layout.fillWidth: true }
            }
        }
    }
}
