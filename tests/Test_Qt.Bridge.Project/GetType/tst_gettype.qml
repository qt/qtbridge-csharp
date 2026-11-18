// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_gettype"

    Foo { id: foo }

    Bar { id: bar }
    MetadataCustomType { id: metadataCustom }
    SourceCustomType { id: sourceCustom }
    MetadataTypeProperty { id: metadataProperty }
    SourceTypeProperty { id: sourceProperty }
    SourceTypeField { id: sourceField }

    function test_discard_return_value() {
        verify(nativeCalls.discardGetType(foo))
        verify(nativeCalls.discardGetType(bar))
    }

    function test_helper_has_priority() {
        compare(metadataCustom.getType(), TypeToStringOf.metadataCustomType)
        compare(sourceCustom.getType(), TypeToStringOf.sourceCustomType)
        compare(metadataProperty.getType(), TypeToStringOf.metadataTypeProperty)
        compare(sourceProperty.getType(), TypeToStringOf.sourceTypeProperty)
        compare(sourceField.getType(), TypeToStringOf.sourceTypeField)
    }

    function test_getType_metadata() {
        compare(foo.getType(), TypeToStringOf.foo)
    }

    function test_getType_source() {
        compare(bar.getType(), TypeToStringOf.bar)
    }
}
