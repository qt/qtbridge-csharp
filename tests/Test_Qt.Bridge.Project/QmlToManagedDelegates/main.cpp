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
    const bool testPathsEnabled = []()
    {
        QStandardPaths::setTestModeEnabled(true);
        return true;
    }();
}

class Setup_QmlToManagedDelegates : public QtQuickTestSetup
{
    Q_OBJECT
};

QUICK_TEST_MAIN_WITH_DOTNET_SETUP(Test_QmlToManagedDelegates, Setup_QmlToManagedDelegates)
#include "main.moc"
