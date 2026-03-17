// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QTest>

#include <qdotnetexception.h>
#include <qdotnetobject.h>
#include <qdotnetsafemethod.h>
#include <qdotnettype.h>

#include "QtTestSetupBase.h"

class Test_ObjectInterop : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
    }

    void createObject()
    {
        const auto newStringBuilder = QDotNetObject::constructor("System.Text.StringBuilder");
        const auto stringBuilder = newStringBuilder();

        QVERIFY(stringBuilder.isValid());

        // Debug-only API in qdotnetadapter.h; the packaged Qt release build hides it.
        // QCOMPARE(QDotNetAdapter::instance().stats().refCount, 1);
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

    void handleException()
    {
        QDotNetSafeMethod<QDotNetObject, int, int> newStringBuilder;
        QDotNetObject::constructor("System.Text.StringBuilder", newStringBuilder);
        const auto stringBuilder = newStringBuilder.invoke(nullptr, 5, 5);
        QString exceptionTypeName;

        try {
            QDotNetSafeMethod<QDotNetObject, QString> append;
            stringBuilder.method("Append", append).invoke(stringBuilder, "Hello");
            QCOMPARE(stringBuilder.toString(), "Hello");
            append.invoke(stringBuilder, " World!");
            QFAIL("Expected Append to throw when exceeding max capacity");
        } catch (const QDotNetException &ex) {
            exceptionTypeName = ex.type().cast<QDotNetType>().fullName();
        }

        QCOMPARE(exceptionTypeName, "System.ArgumentOutOfRangeException");
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_ObjectInterop)
#include "main.moc"
