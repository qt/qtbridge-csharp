// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause
// Icon.qml — IntelliQHome Design System — theme-tinted icon
//
// Not a Figma component: this is the tint *mechanism* that every icon-bearing
// component and screen was repeating by hand. The icon SVGs ship with a
// baked-in fill, so drawing one raw makes it vanish against a background that
// happens to match that fill — every icon has to be recoloured to a Theme
// token. MultiEffect does that, which costs a hidden source Image plus a
// matching effect item; this wraps the pair once.
//
// `size` drives both the drawn size and the raster size, so an Icon is square
// and crisp by default. `resolution` overrides only the raster size, for an
// icon whose drawn size scales or animates.
//
// `source` is a url, so a bare relative string would resolve against *this*
// file's directory inside the Controls module, not the caller's. Wrap call-site
// paths in Qt.resolvedUrl() — including strings that arrive from a model.
//
// Usage:
//   import IntelliQHome.Controls as Ui
//   Ui.Icon { source: Qt.resolvedUrl("assets/icons/wind.svg"); color: Ui.Theme.text_medium }
//   Ui.Icon { source: root.glyph; size: 32 }                // 32px, text_strong
//   Ui.Icon { source: root.glyph; size: 12; rotation: 90 }   // Item rotation
import QtQuick
import QtQuick.Effects
import Weather.Controls

Item {
    id: root

    property url   source: ""
    property int   size: 24
    property color color: Theme.text_strong
    // Raster size for the SVG. Defaults to the drawn size; raise it when the
    // icon is scaled or animated, so the source is rasterised once at the
    // largest size it will reach instead of re-rasterised per frame.
    property int   resolution: size

    implicitWidth:  size
    implicitHeight: size

    Image {
        id: _img
        anchors.centerIn: parent
        width: root.size; height: root.size
        sourceSize { width: root.resolution; height: root.resolution }
        source: root.source
        fillMode: Image.PreserveAspectFit
        visible: false
    }

    MultiEffect {
        anchors.centerIn: parent
        width: root.size; height: root.size
        source: _img
        colorization: 1.0
        colorizationColor: root.color
        // Icons cross-fade with the rest of a control's state change, and with
        // a Light/Dark switch — same step as Button's fill and label.
        Behavior on colorizationColor { ColorAnimation { duration: Theme.duration_fast } }
    }
}
