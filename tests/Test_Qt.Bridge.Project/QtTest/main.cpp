// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QCoreApplication>
#include <QDebug>
#include <QTest>

#include "QtTestSetupBase.h"

class Test_QtTest : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
    }

    void assemblyExists()
    {
        QVERIFY2(QFile::exists(assemblyPath), "Cannot find target assembly");
    }

    void dotnetMain()
    {
        QVERIFY2(runAppSynchronous(), "Managed test entry point failed");
    }

    void initAdapter()
    {
        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
        qInfo() << QString("Hello World from C++!");
    }

    void callStatic()
    {
        auto fortyTwo = QString("%1, %2").arg("QtTest.FortyTwo", assemblyName);
        QCOMPARE(QtDotNet::call<int>(fortyTwo, "get_Value"), 42);
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_QtTest)
#include "main.moc"
