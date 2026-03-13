// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QDebug>
#include <QTest>

#include <qdotnethost.h>

#include "QtTestSetupBase.h"

class Test_FunctionCalls : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    QDotNetFunction<QString, QString, int> formatNumber;

    [[nodiscard]] QString exportsTypeName() const
    {
        return QString("%1, %2").arg("FunctionCalls.FunctionExports", assemblyName);
    }

    [[nodiscard]] QString formatNumberDelegateTypeName() const
    {
        return QString("%1, %2")
            .arg("FunctionCalls.FunctionExports+FormatNumberDelegate", assemblyName);
    }

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
    }

    void resolveFunction()
    {
        QVERIFY(!formatNumber.isValid());
        QVERIFY(dotNetHost->resolveFunction(formatNumber,
            assemblyPath,
            exportsTypeName(),
            "FormatNumber",
            formatNumberDelegateTypeName()));

        QVERIFY(formatNumber.isValid());
    }

    void callFunction()
    {
        QVERIFY(formatNumber.isValid());
        QCOMPARE(formatNumber("[{0}]", 42), "[42]");
    }

    void callDefaultEntryPoint()
    {
        QDotNetFunction<quint32, void *, qint32> entryPoint;
        QVERIFY(dotNetHost->resolveFunction(entryPoint,
            assemblyPath,
            exportsTypeName(),
            "EntryPoint"));

        QVERIFY(entryPoint.isValid());

        QString fortyTwo("42");
        const qint32 returnValue = entryPoint(
            fortyTwo.data(), static_cast<qint32>(fortyTwo.length()));

        QCOMPARE(returnValue, 42);
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_FunctionCalls)
#include "main.moc"
