// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

pragma Singleton

import QtQuick

QtObject {
    readonly property string name: "default"

    readonly property var colors: QtObject {

        readonly property color appBackground: "#F5F7FB"
        readonly property color surface: "white"
        readonly property color border: "#E2E8F0"
        readonly property color mutedText: "#64748B"
        readonly property color accent: "#2563EB"

        readonly property var avatarPalette: [
            avatarRgb(37, 99, 235),
            avatarRgb(124, 58, 237),
            avatarRgb(219, 39, 119),
            avatarRgb(220, 38, 38),
            avatarRgb(234, 88, 12),
            avatarRgb(5, 150, 105),
            avatarRgb(8, 145, 178),
            avatarRgb(71, 85, 105),
        ]

        function avatarRgb(r, g, b) {
            return Qt.rgba(r/255, g/255, b/255, 0.7)
        }

        function avatarColor(idx) {
            return avatarPalette[idx % avatarPalette.length]
        }
    }
}
