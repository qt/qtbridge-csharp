// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import Weather.Controls as Ui

// Theme-aware Figma "WeatherImage" illustration (Style Library → Imagery)
// for a WMO weather code. Transparent PNGs, so no mask/background needed.
Image {
    id: root

    property int weatherCode: 2

    // There is no snow art, so snow codes fall back to the neutral cloud.
    readonly property string _base: {
        const c = root.weatherCode
        if (c === 0)  return "sunny"
        if (c <= 2)   return "partly_cloudy"
        if (c <= 48)  return "cloudy"
        if (c <= 57)  return "partly_rain"
        if (c <= 67)  return "rain"
        if (c <= 77)  return "cloudy"        // snow -- no artwork
        if (c <= 82)  return "heavy_rain"
        if (c <= 86)  return "cloudy"        // snow showers -- no artwork
        if (c <= 99)  return "heavy_rain"    // thunderstorm -- no artwork
        return "cloudy"                      // unknown code
    }

    // Qt.resolvedUrl anchors the relative path to THIS file — a bare string
    // would resolve against the instantiating document's directory instead.
    source: Qt.resolvedUrl("assets/weather/" + _base
                           + (Ui.Theme.isDark ? "_dark" : "_light") + ".png")
    fillMode: Image.PreserveAspectFit
}
