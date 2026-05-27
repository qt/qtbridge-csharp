// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

ApplicationWindow {
    id: mainWindow
    width: 640
    height: 480
    visible: true
    title: "Dynamic Object"

    Foo { id: foo }

    TextArea {
        id: log
        anchors.fill: parent
        readOnly: true
        font.pointSize: 12
    }

    menuBar: MenuBar {
        Menu {
            title: "Foo"
            TestMenu {
                object: "foo"; member: "intProperty"
                Test { expr: " === 42" }
                Test { expr: " = 42" }
            }
            TestMenu {
                object: "foo"; member: "intReadOnlyProperty"
                Test { expr: " === 42" }
                Test { expr: " = 42" }
            }
            TestMenu {
                object: "foo"; member: "intWriteOnlyProperty"
                Test { expr: " === 42" }
                Test { expr: " = 42" }
            }
            TestMenu {
                object: "foo"; member: "stringProperty"
                Test { expr: " === 'foobar'" }
                Test { expr: " = 'foobar'" }
            }
            TestMenu {
                object: "foo"; member: "dateTimeProperty"
                Test { }
                Test { expr: " = new Date()" }
            }
            TestMenu {
                object: "foo"; member: "uriProperty"
                Test { }
                Test { expr: " = 'https://qt.io'" }
            }
            TestMenu {
                object: "foo"; member: "uInt64FuncInt"
                Test { expr: "(83) === 99194853094755497" }
            }
        }
    }
}
