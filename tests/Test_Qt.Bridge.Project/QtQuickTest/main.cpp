// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QCoreApplication>
#include <QDebug>
#include <QStandardPaths>
#include <QQmlEngine>
#include <QtQuickTest>

#include "QtQuickTestSetup.h"

namespace
{
    // Enable Qt test-mode paths before Qt's
    // generated main() creates the app object.
    const bool testPathsEnabled = []()
    {
        QStandardPaths::setTestModeEnabled(true);
        return true;
    }();
}

class Setup_QtQuickTest : public QtQuickTestSetup
{
    Q_OBJECT

protected:
    void afterQmlEngineAvailable(QQmlEngine* /*qmlEngine*/) override
    {
        qInfo() << QString("Hello World from C++!");
    }
};

QUICK_TEST_MAIN_WITH_SETUP(Test_QtQuickTest, Setup_QtQuickTest)
#include "main.moc"
