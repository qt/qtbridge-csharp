// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QCoreApplication>
#include <QDebug>
#include <QTest>

#include <qdotnetobject.h>
#include <qdotnetstatic.h>
#include <qdotnettype.h>

#include "QtTestSetupBase.h"

class Test_AdapterInit : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
    }

    void dotnetMain()
    {
        QVERIFY2(runAppSynchronous(), "Managed test entry point failed");
    }

    void initAdapter()
    {
        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
        qInfo() << "AdapterInit native host ready";
    }

    void callManagedStaticProperty()
    {
        const auto fortyTwoType = QString("%1, %2").arg("AdapterInit.FortyTwo", assemblyName);
        QCOMPARE(QtDotNet::call<int>(fortyTwoType, "get_Value"), 42);
    }

    void callStaticMethod()
    {
        const auto environment = QDotNetType::typeOf("System.Environment");
        const auto getEnvironmentVariable
            = environment.staticMethod<QString, QString>("GetEnvironmentVariable");
        const auto path = getEnvironmentVariable("PATH");
        QVERIFY2(!path.isEmpty(), "PATH environment variable should not be empty");
        auto value = QtDotNet::call<QString, QString>(
            "System.Environment",
            "GetEnvironmentVariable",
            "PATH");
        QCOMPARE(value, path);
    }

    void callInstanceMethod()
    {
        const auto newStringBuilder = QDotNetObject::constructor("System.Text.StringBuilder");
        const auto stringBuilder = newStringBuilder();
        const auto append = stringBuilder.method<QDotNetObject, QString>("Append");
        std::ignore = append("Hello");
        std::ignore = append(" World!");
        QCOMPARE(stringBuilder.toString(), "Hello World!");
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN_WITH_DOTNET_SETUP(Test_AdapterInit)
#include "main.moc"
