// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause
//
// Main.qml — WeatherApp: a standalone shell around the Weather panel
// stripped out of kaira-home-hub (no house view, no device manager, no
// side-panel navigation).

import QtQuick
import QtQuick.Window
import QtQuick.Layouts
import Weather
import Weather.Controls as Ui

Window {
    id: window
    width: 1000
    height: 778
    minimumWidth: 700
    minimumHeight: 560
    visible: true
    title: qsTr("Weather")

    // Touching FontSetup instantiates it: QML singletons are lazy, and its
    // FontLoaders must exist before anything draws with these fonts. Theme
    // defaults to light, and the toggle is this app's only way to change it.
    Component.onCompleted: {
        FontSetup.body
        Ui.Theme.isDark = true
        WeatherManager.locate()
        WeatherManager.fetch()

        // Center the window on the primary screen
        x = (Screen.width - width) / 2
        y = (Screen.height - height) / 2
    }

    Rectangle {
        anchors.fill: parent
        color: Ui.Theme.background_1
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        RowLayout {
            Layout.fillWidth: true
            Layout.margins: 16

            Ui.Toggle {
                text: qsTr("Dark mode")
                checked: Ui.Theme.isDark
                onToggled: Ui.Theme.isDark = checked
            }
            Item { Layout.fillWidth: true }
        }

        Weather {
            Layout.fillWidth: true
            Layout.fillHeight: true
        }
    }
}
