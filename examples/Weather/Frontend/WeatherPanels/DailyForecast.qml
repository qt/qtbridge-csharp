// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui

// Figma daily-forecast card (node 166:3354; titled "Hourly forecast today"
// in the mock — an obvious copy/paste slip, kept as "Daily forecast").
Rectangle {
    color: Ui.Theme.background_2
    radius: Ui.Spacing.radius_24
    implicitHeight: dailyCol.implicitHeight + 32

    // Aggregated in C#, where the forecast is a List. Here it is a list
    // model, bound through the delegate below rather than iterated.
    readonly property string summaryText: WeatherManager.loading ? "" : WeatherManager.dailyForecastSummary

    ColumnLayout {
        id: dailyCol
        anchors.fill: parent
        anchors.margins: 16
        spacing: 16

        Text {
            Layout.fillWidth: true
            text: "Daily forecast"
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

            // "Today" is dropped in C#, for the same reason as summaryText.
            model: WeatherManager.dailyForecastUpcoming

            delegate: ColumnLayout {
                required property string day
                required property int weatherCode
                required property double tempMax
                required property double windMax

                width: ListView.view.width / Math.max(1, Math.min(ListView.view.count, 6))
                height: ListView.view.height
                spacing: 8

                Text {
                    Layout.alignment: Qt.AlignHCenter
                    text: day
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
                    text: tempMax.toFixed(0) + "°"
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
                        text: windMax.toFixed(0)
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
