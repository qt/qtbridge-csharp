// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui

// Figma "Hourly forecast today" card (node 166:3213): title, condition
// summary, and a row of hour columns (time / icon / temp / wind).
Rectangle {
    color: Ui.Theme.background_2
    radius: Ui.Spacing.radius_24
    implicitHeight: hourlyCol.implicitHeight + 32

    readonly property string summaryText: {
        if (WeatherManager.loading) return ""
        var desc = WeatherManager.weatherDescription(WeatherManager.weatherCode)
        return desc + " conditions today. Wind gusts are up to "
                + WeatherManager.windGusts.toFixed(0) + " km/h."
    }

    ColumnLayout {
        id: hourlyCol
        anchors.fill: parent
        anchors.margins: 16
        spacing: 16

        Text {
            Layout.fillWidth: true
            text: "Hourly forecast today"
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.h3_size              // 24 SemiBold
            font.weight:    Ui.Typography.weight_semibold
            color:          Ui.Theme.text_strong
        }

        Text {
            Layout.fillWidth: true
            text: summaryText
            elide: Text.ElideRight
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.body_2_size          // 16 Light
            font.weight:    Ui.Typography.weight_light
            color:          Ui.Theme.text_medium
        }

        ListView {
            Layout.fillWidth: true
            Layout.preferredHeight: 150
            orientation: Qt.Horizontal
            spacing: 0
            clip: true

            model: WeatherManager.hourlyForecast

            delegate: ColumnLayout {
                required property string time
                required property int weatherCode
                required property double temperature
                required property double windSpeed
                required property bool isNow

                width: ListView.view.width / Math.min(ListView.view.count, 10)
                height: ListView.view.height
                spacing: 8

                Text {
                    Layout.alignment: Qt.AlignHCenter
                    text: isNow ? "Now" : time
                    font.family:    Ui.Typography.font_body
                    font.pixelSize: Ui.Typography.body_2_size  // 16 Light
                    font.weight:    Ui.Typography.weight_light
                    color:          Ui.Theme.text_medium
                }

                Item { Layout.fillHeight: true }

                Text {
                    Layout.alignment: Qt.AlignHCenter
                    text: WeatherManager.weatherEmoji(weatherCode)
                    font.pixelSize: 24
                }

                Item { Layout.fillHeight: true }

                Text {
                    Layout.alignment: Qt.AlignHCenter
                    text: temperature.toFixed(0) + "°"
                    font.family:    Ui.Typography.font_body
                    font.pixelSize: Ui.Typography.label_2_size // 16 Medium
                    font.weight:    Ui.Typography.weight_medium
                    color:          Ui.Theme.text_medium
                }

                RowLayout {
                    Layout.alignment: Qt.AlignHCenter
                    spacing: 2
                    Ui.Icon {
                        size: 12
                        source: Qt.resolvedUrl("../assets/icons/wind_arrow.svg")
                        color: Ui.Theme.text_medium
                    }
                    Text {
                        text: windSpeed.toFixed(0)
                        font.family:    Ui.Typography.font_body
                        font.pixelSize: Ui.Typography.label_4_size // 12 Medium
                        font.weight:    Ui.Typography.weight_medium
                        color:          Ui.Theme.text_medium
                    }
                }
            }
        }
    }
}
