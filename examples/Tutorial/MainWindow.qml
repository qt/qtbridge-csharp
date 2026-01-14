// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ApplicationWindow {
    id: window; width: 220; height: 240; visible: true; title: "Names"

    NameList { id: names }

    ColumnLayout {
        anchors.fill: parent

        TextField {
            Layout.fillWidth: true; leftPadding: 10; focus: true;
            placeholderText: "Enter a name"
            onAccepted: {
                let name = text.trim();
                if (name)
                    names.add(name);
                clear();
            }
        }

        ListView {
            model: names
            Layout.fillWidth: true; Layout.fillHeight: true; clip: true
            delegate: Text {
                required property string item
                text: item; leftPadding: 10
            }
        }
    }
}
