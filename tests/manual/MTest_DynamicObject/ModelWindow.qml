// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

Window {
    id: root

    required property var fixtures

    width: 475
    height: 600
    visible: false
    minimumWidth: 475
    title: "Model view"
    color: "#f3f4f6"

    readonly property var currentFixture: fixtures[fixtureSelector.currentIndex]

    // Map fixture kinds to their build/load pane components.
    readonly property var paneComponents: ({
        "tree": { build: treeModelPaneBuild, load: treeModelPaneLoad }
    })

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 20
        spacing: 12

        RowLayout {
            Layout.fillWidth: true
            spacing: 10

            Label {
                text: "Model fixture:"
            }
            ComboBox {
                id: fixtureSelector
                Layout.preferredWidth: 200
                model: root.fixtures.map(fixture => fixture.name)
                enabled: root.fixtures.length > 1
            }
            Item {
                Layout.fillWidth: true
            }
        }

        Label {
            Layout.fillWidth: true
            text: "Expected: " + root.currentFixture.expected
            wrapMode: Text.WordWrap
            color: "#5f6368"
        }

        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            spacing: 20

            Loader {
                Layout.fillWidth: true
                Layout.fillHeight: true
                Layout.preferredWidth: 0
                sourceComponent: root.paneComponents[root.currentFixture.kind]?.build ?? null
            }
            Loader {
                Layout.fillWidth: true
                Layout.fillHeight: true
                Layout.preferredWidth: 0
                sourceComponent: root.paneComponents[root.currentFixture.kind]?.load ?? null
            }
        }
    }

    Component {
        id: treeModelPaneBuild
        TreeModelPane {
            label: "Build-time (bt) — source-generated"
            model: root.currentFixture.buildModel
        }
    }
    Component {
        id: treeModelPaneLoad
        TreeModelPane {
            label: "Load-time (lt) — metadata-driven"
            model: root.currentFixture.loadModel
        }
    }
}
