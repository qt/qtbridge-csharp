// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause
// Spacing.qml — IntelliQHome Design System
// Source: design-tokens.json (extracted from Figma 'Style Library for "CE-DEMO"')
// GENERATED FILE — edit design-tokens.json and re-run extraction, not this file.
// CMake: set_source_files_properties(qml/Spacing.qml PROPERTIES QT_QML_SINGLETON_TYPE TRUE)
// Corner radii (from Figma Radius collection) + a 4px-grid spacing scale.
// NOTE: the Figma system defines no dedicated spacing variables; the spacing
//       scale below is a 4px-grid convenience aligned with the radius steps.
// Usage: radius: Spacing.radius_m ; spacing: Spacing.md
pragma Singleton
import QtQuick

QtObject {
    // ── Corner radii (Figma-verified) ───────────────────────────────
    readonly property int radius_4: 4
    readonly property int radius_8: 8
    readonly property int radius_12: 12
    readonly property int radius_16: 16
    readonly property int radius_24: 24
    readonly property int radius_32: 32
    readonly property int radius_40: 40
    readonly property int radius_48: 48
    readonly property int radius_full: 999

    // ── Semantic radius aliases ─────────────────────────────────────
    readonly property int radius_s: radius_8
    readonly property int radius_m: radius_16
    readonly property int radius_l: radius_24
    readonly property int radius_xl: radius_32

    // ── Spacing scale (4px grid) ────────────────────────────────────
    readonly property int xxs: 4
    readonly property int xs: 8
    readonly property int sm: 12
    readonly property int md: 16
    readonly property int lg: 24
    readonly property int xl: 32
    readonly property int xxl: 40
    readonly property int xxxl: 48
}
