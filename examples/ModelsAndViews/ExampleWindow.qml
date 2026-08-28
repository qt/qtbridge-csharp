// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

ApplicationWindow {
    width: 300; height: 280; visible: true
    background: Rectangle { color: "white" }

    // Arranges the example windows in a 2x2 grid on screen.
    property int screenColumn: 0 // 0: left, 1: right
    property int screenRow: 0 // 0: top, 1: bottom

    Component.onCompleted: {
        x = screenColumn === 0 ? (screen.width - 2 * width - 20) / 2 : (screen.width + 20) / 2
        y = screenRow === 0 ? (screen.height - 2 * height - 40) / 2 : (screen.height + 40) / 2
    }

    function windowWidth() { return width }
}
