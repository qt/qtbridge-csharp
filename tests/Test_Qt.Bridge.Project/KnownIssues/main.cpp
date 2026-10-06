// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QFileInfo>
#include <QStandardPaths>
#include <QtQuickTest>

#include "QtQuickTestSetup.h"
#include "metadata_loader.h"
#include "qml_register_types.h"

namespace
{
    const bool testPathsEnabled = []()
    {
        QStandardPaths::setTestModeEnabled(true);
        return true;
    }();
}

class Setup : public QtQuickTestSetup
{
    Q_OBJECT
};

int main(int argc, char **argv)
{
    QDotNetConvert::setDispatch(QtDotNet::objectDispatch);
    QtDotNet::loadTypeMetadata(QFileInfo(argv[0]).absoluteDir().path(), qml_register_types);
    QTEST_SET_MAIN_SOURCE_PATH
    Setup setup;
    return quick_test_main_with_setup(argc, argv, "Test_KnownIssues",
                                      QUICK_TEST_SOURCE_DIR_DOTNET, &setup);
}

#include "main.moc"
