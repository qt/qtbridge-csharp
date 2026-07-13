// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls

Menu {
    id: test
    required property string object
    TestMenu {
        object: test.object; member: "toString"; symbol: "𝑓"
        Test { text: "()" }
    }
    TestMenu {
        object: test.object; member: "intProperty"; symbol: "𝑥"
        Test { text: " === 42" }
        Test { text: " = 42" }
    }
    TestMenu {
        object: test.object; member: "intReadOnlyProperty"; symbol: "𝑥"
        Test { text: " === 42" }
        Test { text: " = 42" }
    }
    TestMenu {
        object: test.object; member: "intWriteOnlyProperty"; symbol: "𝑥"
        Test { text: " === 42" }
        Test { text: " = 42" }
    }
    TestMenu {
        object: test.object; member: "stringProperty"; symbol: "𝑥"
        Test { text: " === 'foobar'" }
        Test { text: " = 'foobar'" }
    }
    TestMenu {
        object: test.object; member: "dateTimeProperty"; symbol: "𝑥"
        Test { }
        Test { text: " = new Date()" }
    }
    TestMenu {
        object: test.object; member: "uriProperty"; symbol: "𝑥"
        Test { }
        Test { text: " = 'https://qt.io'" }
    }
    TestMenu {
        object: test.object; member: "uInt64FuncInt"; symbol: "𝑓"
        Test { text: "(83) === 99194853094755497" }
    }
    TestMenu {
        object: test.object; member: "buildTimeTypeObj"; symbol: "𝑥"
        Test { text: ".toString()" }
        Test { text: ".equals(" + test.object + ")" }
    }
    TestMenu {
        object: test.object; member: "loadTimeTypeObj"; symbol: "𝑥"
        Test { text: ".toString()" }
        Test { text: ".equals(" + test.object + ")" }
    }
}
