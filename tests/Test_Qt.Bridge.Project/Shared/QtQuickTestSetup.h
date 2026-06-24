// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#pragma once

#include <QObject>
#include <QQmlEngine>
#include <QtQuickTest>

#include "QtTestSetupBase.h"

#ifdef QUICK_TEST_SOURCE_DIR
#  define QUICK_TEST_SOURCE_DIR_DOTNET QUICK_TEST_SOURCE_DIR
#else
#  define QUICK_TEST_SOURCE_DIR_DOTNET nullptr
#endif

#define QUICK_TEST_MAIN_WITH_DOTNET_SETUP(name, QuickTestSetupClass)                       \
    int main(int argc, char **argv)                                                        \
    {                                                                                      \
        QDotNetConvert::setDispatch(QtDotNet::objectDispatch);                             \
        QTEST_SET_MAIN_SOURCE_PATH                                                         \
        QuickTestSetupClass setup;                                                         \
        return quick_test_main_with_setup(argc, argv, #name, QUICK_TEST_SOURCE_DIR_DOTNET, \
                                          &setup);                                         \
    }

class QtQuickTestSetup : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

public slots:
    void applicationAvailable();
    void qmlEngineAvailable(QQmlEngine* qmlEngine);
    void cleanupTestCase();

protected:
    virtual bool beforeApplicationAvailable();
    virtual void afterApplicationAvailable();

    virtual bool beforeQmlEngineAvailable(QQmlEngine* engine);
    virtual void afterQmlEngineAvailable(QQmlEngine* engine);

    virtual bool beforeCleanupTestCase();
    virtual void afterCleanupTestCase();

    virtual bool handleReadyResult(ReadyResult result);
    virtual bool handleFinalizeResult(FinalizeResult result);
};
