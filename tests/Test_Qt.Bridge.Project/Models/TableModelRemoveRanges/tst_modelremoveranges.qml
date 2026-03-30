// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_modelremoveranges"

    RemoveRangeTableModel {
        id: model
    }

    function init() {
        model.resetLastRemoveRanges()
    }

    function test_removeRows_reportsInclusiveRange() {
        verify(model.removeRowsViaModelApi(5, 2))
        compare(model.lastRowRemoveFirst, 5)
        compare(model.lastRowRemoveLast, 6)
    }

    function test_removeColumns_reportsInclusiveRange() {
        verify(model.removeColumnsViaModelApi(4, 3))
        compare(model.lastColumnRemoveFirst, 4)
        compare(model.lastColumnRemoveLast, 6)
    }
}
