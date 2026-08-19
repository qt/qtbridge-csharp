// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtQuick.Controls
import QtTest
import Application

TreeView {
    id: view
    width: 100
    height: 100
    visible: true

    property bool viewCompleted: false
    property bool childVisible: false
    Component.onCompleted: viewCompleted = true

    model: MetadataTree {
        id: data
    }

    TestCase {
        name: "tst_treemodel"
        when: view.viewCompleted

        function test_expand_metadata_tree() {
            verify(view.expand(0))
            view.forceLayout()
            tryVerify(function() { return view.childVisible })
        }
    }

    delegate: TreeViewDelegate {
        contentItem: Label {
            text: model.name
            Component.onCompleted: {
                if (text === "Qt")
                    view.childVisible = true
            }
        }
    }
}
