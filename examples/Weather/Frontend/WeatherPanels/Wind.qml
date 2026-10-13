// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui

// Figma "wind" card (node 166:3317): compact title + gusts/direction line.
Rectangle {
    color: Ui.Theme.background_2
    radius: Ui.Spacing.radius_24
    implicitHeight: windCol.implicitHeight + 32

    ColumnLayout {
        id: windCol
        anchors.fill: parent
        anchors.margins: 16
        spacing: 16

        Text {
            text: "Wind"
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.h4_size              // 20 SemiBold
            font.weight:    Ui.Typography.weight_semibold
            color:          Ui.Theme.text_strong
        }

        Text {
            Layout.fillWidth: true
            text: WeatherManager.loading
                  ? "" : "Gusts " + WeatherManager.windGusts.toFixed(0) + " km/h "
                         + WeatherManager.windDirectionLabel(WeatherManager.windDirection)
            font.family:    Ui.Typography.font_body
            font.pixelSize: Ui.Typography.body_2_size          // 16 Light
            font.weight:    Ui.Typography.weight_light
            color:          Ui.Theme.text_medium
        }
    }
}
