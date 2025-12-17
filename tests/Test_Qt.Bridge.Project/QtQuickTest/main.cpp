/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include <QCoreApplication>
#include <QDebug>
#include <QQmlEngine>
#include <QtQuickTest>

#include "QtQuickTestSetup.h"

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
