// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

import DefaultStyle as AppStyle

Item {
    id: loadingView
    property bool animateIndicator: false
    ColumnLayout {
        anchors.centerIn: parent
        spacing: 0
        BusyIndicator {
            Layout.alignment: Qt.AlignHCenter | Qt.AlignVCenter
            running: loadingView.animateIndicator
        }
        AppStyle.Label {
            Layout.alignment: Qt.AlignHCenter | Qt.AlignVCenter
            Layout.topMargin: 18
            text: "Loading people..."
            font.pixelSize: 26
        }
    }
}
