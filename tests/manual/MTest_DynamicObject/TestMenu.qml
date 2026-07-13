// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

Menu {
    required property string symbol
    icon.source: `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128px" height="128px"><text style="fill: rgb(0, 65, 74); font-family: Arial, sans-serif; font-size: 100px; white-space: pre;" x="37.291" y="98.339">` + symbol + `</text></svg>`
    icon.width: 16
    icon.height: 16
    required property string object
    required property string member
    title: object + "." + member
}
