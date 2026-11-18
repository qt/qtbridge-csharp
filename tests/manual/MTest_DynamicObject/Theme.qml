// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

pragma Singleton

import QtQuick

QtObject {
    readonly property QtObject colors: QtObject {
        readonly property color windowBackground: "#f3f4f6"

        readonly property color statusColor: "#5f6368"
        readonly property color statusColorPass: "#1e7d34"
        readonly property color statusColorFail: "#c62828"

        readonly property color statusBackground: "#e9eaec"
        readonly property color statusBackgroundPass: "#e3f5e8"
        readonly property color statusBackgroundFail: "#fdeaea"

        readonly property color rowColor0: "#ffffff"
        readonly property color rowColor1: "#f6f7f8"
    }
}
