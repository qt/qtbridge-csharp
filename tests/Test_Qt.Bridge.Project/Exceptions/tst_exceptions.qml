// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

import QtQuick
import QtTest
import Application

TestCase {
    name: "tst_exceptions"

    MetadataExceptionGenerator { id: objMetadata }
    SourceCodeExceptionGenerator { id: objSourceCode }

    function test_metadataExceptionMethod() {
        try {
            objMetadata.exceptionMethod();
        } catch (error) {
            warn(error.message);
            return;
        }
        fail("Exception ignored");
    }

    function test_metadataExceptionPropertyGet() {
        try {
            console.log(objMetadata.exceptionProperty);
        } catch (error) {
            warn(error.message);
            return;
        }
        fail("Exception ignored");
    }

    function test_metadataExceptionPropertySet() {
        try {
            objMetadata.exceptionProperty = 42;
        } catch (error) {
            warn(error.message);
            return;
        }
        fail("Exception ignored");
    }

    function test_sourceExceptionMethod() {
        try {
            objSourceCode.exceptionMethod();
        } catch (error) {
            warn(error.message);
            return;
        }
        fail("Exception ignored");
    }

    function test_sourceExceptionPropertyGet() {
        try {
            console.log(objSourceCode.exceptionProperty);
        } catch (error) {
            warn(error.message);
            return;
        }
        fail("Exception ignored");
    }

    function test_sourceExceptionPropertySet() {
        try {
            objSourceCode.exceptionProperty = 42;
        } catch (error) {
            warn(error.message);
            return;
        }
        fail("Exception ignored");
    }
}
