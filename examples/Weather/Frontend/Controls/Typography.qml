// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause
// Typography.qml — IntelliQHome Design System
// Source: design-tokens.json (extracted from Figma 'Style Library for "CE-DEMO"')
// GENERATED FILE — edit design-tokens.json and re-run extraction, not this file.
// CMake: set_source_files_properties(qml/Typography.qml PROPERTIES QT_QML_SINGLETON_TYPE TRUE)
// Font families, weights and the type scale (from Figma text styles).
// NOTE: families 'Spartan' (display) and 'Inter' (body) must be installed or
//       bundled via a FontLoader; otherwise Qt falls back to a system font.
// Usage: font.family: Typography.font_body
//        font.pixelSize: Typography.h1_size ; font.weight: Typography.h1_weight
pragma Singleton
import QtQuick

QtObject {
    // ── Font families ───────────────────────────────────────────────
    readonly property string font_display: "League Spartan"
    readonly property string font_body: "Inter"

    // ── Font weights ────────────────────────────────────────────────
    readonly property int weight_light: 300
    readonly property int weight_medium: 500
    readonly property int weight_semibold: 600

    // ── Type scale ──────────────────────────────────────────────────
    readonly property string display_1_family: font_display
    readonly property int display_1_size: 120
    readonly property int display_1_weight: weight_medium
    readonly property int display_1_line_height: 136

    readonly property string display_2_family: font_display
    readonly property int display_2_size: 72
    readonly property int display_2_weight: weight_medium
    readonly property int display_2_line_height: 80

    readonly property string display_3_family: font_display
    readonly property int display_3_size: 48
    readonly property int display_3_weight: weight_medium
    readonly property int display_3_line_height: 54

    readonly property string display_4_family: font_display
    readonly property int display_4_size: 32
    readonly property int display_4_weight: weight_semibold
    readonly property int display_4_line_height: 36

    // Header line heights are 100% of size since Figma 2026-07.
    readonly property string h1_family: font_body
    readonly property int h1_size: 48
    readonly property int h1_weight: weight_semibold
    readonly property int h1_line_height: 48

    readonly property string h2_family: font_body
    readonly property int h2_size: 32
    readonly property int h2_weight: weight_semibold
    readonly property int h2_line_height: 32

    readonly property string h3_family: font_body
    readonly property int h3_size: 24
    readonly property int h3_weight: weight_semibold
    readonly property int h3_line_height: 24

    readonly property string h4_family: font_body
    readonly property int h4_size: 20
    readonly property int h4_weight: weight_semibold
    readonly property int h4_line_height: 20

    readonly property string h5_family: font_body
    readonly property int h5_size: 16
    readonly property int h5_weight: weight_semibold
    readonly property int h5_line_height: 16
    readonly property int h5_letter_spacing: 3

    readonly property string body_1_family: font_body
    readonly property int body_1_size: 20
    readonly property int body_1_weight: weight_light
    readonly property int body_1_line_height: 24

    readonly property string body_2_family: font_body
    readonly property int body_2_size: 16
    readonly property int body_2_weight: weight_light
    readonly property int body_2_line_height: 20

    readonly property string body_3_family: font_body
    readonly property int body_3_size: 14
    readonly property int body_3_weight: weight_light
    readonly property int body_3_line_height: 18

    readonly property string body_4_family: font_body
    readonly property int body_4_size: 12
    readonly property int body_4_weight: weight_light
    readonly property int body_4_line_height: 16

    readonly property string body_5_family: font_body
    readonly property int body_5_size: 10
    readonly property int body_5_weight: weight_light
    readonly property int body_5_line_height: 12

    readonly property string label_1_family: font_body
    readonly property int label_1_size: 20
    readonly property int label_1_weight: weight_medium
    readonly property int label_1_line_height: 24

    readonly property string label_2_family: font_body
    readonly property int label_2_size: 16
    readonly property int label_2_weight: weight_medium
    readonly property int label_2_line_height: 20

    readonly property string label_3_family: font_body
    readonly property int label_3_size: 14
    readonly property int label_3_weight: weight_medium
    readonly property int label_3_line_height: 18

    readonly property string label_4_family: font_body
    readonly property int label_4_size: 12
    readonly property int label_4_weight: weight_medium
    readonly property int label_4_line_height: 16

    readonly property string label_5_family: font_body
    readonly property int label_5_size: 10
    readonly property int label_5_weight: weight_medium
    readonly property int label_5_line_height: 12

}
