// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QTest>
#include <qdotnettype.h>
#include "QtTestSetupBase.h"

class Test_InboundMemLeak : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    QDotNetFunction<INBOUND_TYPE> fnInbound = nullptr;
    QDotNetFunction<double> fnCorrelation = nullptr;

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
        fnInbound = QDotNetType::staticMethod<INBOUND_TYPE>(TYPE_NAME, INBOUND_FUNC);
        QVERIFY(fnInbound.isValid());
        fnCorrelation = QDotNetType::staticMethod<double>(TYPE_NAME, "Correlation");
        QVERIFY(fnCorrelation.isValid());
    }

    void loopInbound()
    {
        QBENCHMARK {
            fnInbound();
        }
    }

    void checkCorrelation()
    {
        double r = fnCorrelation();
        qInfo() << "r:" << r;
        QCOMPARE_LT(r, 0.5);
    }

    void cleanupTestCase() { unloadHost(); }
};

QTEST_MAIN_WITH_DOTNET_SETUP(Test_InboundMemLeak)
#include "main.moc"
