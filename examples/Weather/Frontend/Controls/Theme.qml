// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause
// Theme.qml — IntelliQHome Design System
// Source: design-tokens.json (extracted from Figma 'Style Library for "CE-DEMO"')
// GENERATED FILE — edit design-tokens.json and re-run extraction, not this file.
// CMake: set_source_files_properties(qml/Theme.qml PROPERTIES QT_QML_SINGLETON_TYPE TRUE)
// Semantic color tokens with Light/Dark modes. Toggle Theme.isDark at runtime.
// Usage: color: Theme.background_1 ; border.color: Theme.stroke_default
pragma Singleton
import QtQuick

QtObject {
    // Runtime theme switch — true = Dark mode, false = Light mode.
    property bool isDark: false

    // ── Layout ──────────────────────────────────────────────────────
    // Width (px) at which a device view switches to its wide, two-column layout.
    readonly property real expandedThreshold: 600

    // ── Motion ──────────────────────────────────────────────────────
    // NOTE: the Figma system defines no duration variables; this is the single
    //       fast-transition step the controls use for state changes
    //       (hover/press/checked colour, opacity and size cross-fades).
    //       Hand-authored like expandedThreshold above — not from extraction.
    readonly property int duration_fast: 120

    // ── Base ────────────────────────────────────────────────────────
    readonly property color base_full_black: Primitives.platinum.platinum_950   // Light re-aliased powder_petal_950 -> platinum_950 in Figma 2026-08
    readonly property color base_full_white: isDark ? "#ffffff" : "#ffffff"

    // ── Highlight / Accent ──────────────────────────────────────────
    readonly property color highlight_strong: isDark ? Primitives.shamrock.shamrock_600 : Primitives.powder_petal.powder_petal_300
    readonly property color highlight_medium: isDark ? Primitives.shamrock.shamrock_700 : Primitives.powder_petal.powder_petal_400
    readonly property color highlight_light: isDark ? Primitives.shamrock.shamrock_900 : Primitives.powder_petal.powder_petal_600

    // ── Control ─────────────────────────────────────────────────────
    readonly property color control_inactive: isDark ? Primitives.ocean.ocean_800 : Primitives.platinum.platinum_300

    // ── Text / Icon ─────────────────────────────────────────────────
    readonly property color text_strong: isDark ? "#ffffff" : Primitives.platinum.platinum_950
    readonly property color text_medium: Primitives.platinum.platinum_500   // Mode-independent since Figma 2026-07
    readonly property color text_subtle: isDark ? Primitives.platinum.platinum_700 : Primitives.platinum.platinum_300
    readonly property color text_invert: isDark ? Primitives.platinum.platinum_950 : "#ffffff"

    // ── Backgrounds ─────────────────────────────────────────────────
    readonly property color background_1: isDark ? Primitives.ocean.ocean_950 : "#ffffff"   // Figma 'Foreground/Main'
    readonly property color background_2: isDark ? Primitives.ocean.ocean_900 : Primitives.platinum.platinum_100   // Figma 'Foreground/Secondary'
    readonly property color background_main: isDark ? Primitives.ocean.ocean_950 : Primitives.powder_petal.powder_petal_50   // Figma 'Backgrounds/Main' (new 2026-07)
    readonly property color background_secondary: isDark ? Primitives.ocean.ocean_900 : "#ffffff"   // Figma 'Backgrounds/Secondary' (new 2026-07)
    readonly property color foreground_highlight: isDark ? Primitives.shamrock.shamrock_600 : Primitives.powder_petal.powder_petal_400   // Figma 'Foreground/Highlight' (new 2026-07)

    // ── Gradients ───────────────────────────────────────────────────
    readonly property color gradient_start_1: isDark ? Primitives.shamrock.shamrock_900 : Primitives.powder_petal.powder_petal_200
    readonly property color gradient_end_1: isDark ? Primitives.shamrock.shamrock_400 : Primitives.powder_petal.powder_petal_600
    readonly property color gradient_start_3: isDark ? Primitives.basics.basics_full_black : "#ffffff"
    readonly property color gradient_end_3: isDark ? Primitives.ocean.ocean_900 : Primitives.powder_petal.powder_petal_100

    // ── Status ──────────────────────────────────────────────────────
    readonly property color status_success: isDark ? Primitives.green.green_500 : Primitives.green.green_600
    readonly property color status_success_muted: isDark ? Primitives.green.green_700 : Primitives.green.green_400
    readonly property color status_success_subtle: isDark ? Primitives.green.green_950 : Primitives.green.green_100
    readonly property color status_warning: isDark ? Primitives.amber.amber_500 : Primitives.amber.amber_600   // Figma 'Status/Alert'
    readonly property color status_warning_muted: isDark ? Primitives.amber.amber_700 : Primitives.amber.amber_400
    readonly property color status_warning_subtle: isDark ? Primitives.amber.amber_950 : Primitives.amber.amber_100
    readonly property color status_error: isDark ? Primitives.red.red_500 : Primitives.red.red_600
    readonly property color status_error_muted: isDark ? Primitives.red.red_700 : Primitives.red.red_400
    readonly property color status_error_subtle: isDark ? Primitives.red.red_950 : Primitives.red.red_100
    readonly property color status_info: isDark ? Primitives.blue.blue_500 : Primitives.blue.blue_600   // Figma 'Status/Info' family (new 2026-08)
    readonly property color status_info_muted: isDark ? Primitives.blue.blue_700 : Primitives.blue.blue_400
    readonly property color status_info_subtle: isDark ? Primitives.blue.blue_950 : Primitives.blue.blue_100

    // ── Stroke ──────────────────────────────────────────────────────
    readonly property color stroke_default: isDark ? Primitives.ocean.ocean_700 : Primitives.platinum.platinum_300   // Dark re-aliased ocean_800 -> ocean_700 in Figma 2026-08
    readonly property color stroke_muted: isDark ? Primitives.ocean.ocean_800 : Primitives.platinum.platinum_200   // Figma 'Stroke/Muted' (new 2026-08)
    readonly property color stroke_subtle: isDark ? Primitives.ocean.ocean_900 : Primitives.platinum.platinum_100

}
