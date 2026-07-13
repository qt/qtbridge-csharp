// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

MenuItem {
    icon.source: menu.icon.source
    icon.width: menu.icon.width
    icon.height: menu.icon.height
    readonly property string expr: menu.title + text
    onTriggered: evaluate(expr)
}
