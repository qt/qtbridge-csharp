// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QCoreApplication>
#include <QDebug>
#include <QTest>

#include <qdotnetobject.h>
#include <qdotnetsafemethod.h>
#include <qdotnettype.h>

#include "QtTestSetupBase.h"

class Test_QtResources : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    QString validatorType;

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(runAppSynchronous(), "Managed app entry point failed");
        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
        validatorType = QString("QtResources.ResourceValidator, %1").arg(assemblyName);
    }

    void resourceExists()
    {
        QVERIFY2(QtDotNet::call<bool>(validatorType, "CheckExists"),
            "Qt.Resources.Exists returned false for a packaged resource");
    }

    void resourceContent()
    {
        QVERIFY2(QtDotNet::call<bool>(validatorType, "CheckContent"),
            "Qt.Resources.ReadAllText returned unexpected content");
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_QtResources)
#include "main.moc"
