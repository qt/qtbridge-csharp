// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

MenuItem {
    property string expr: ""
    readonly property string testExpr: menu.title + expr
    text: expr ? expr : menu.title
    onTriggered: {
        try {
            let result = eval(testExpr)
            if (result)
                log.text += "\u2714 " + testExpr + " \u279C " + result + "\n"
            else
                log.text += "\u2718 " + testExpr + " \u279C " + result + "\n"
        } catch (error) {
            log.text += "\u26A0 " + testExpr + "\n"
            log.text += "\u26A0 " + error + "\n"
        }
    }
}
