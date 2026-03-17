// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QTest>

#include "QtTestSetupBase.h"
#include "stringbuilder.h"
#include "uri.h"

class Test_WrapperTypes : public QObject, protected QtTestSetupBase
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

    void useWrapperClassForStringBuilder()
    {
        StringBuilder sb;

        // Debug-only API in qdotnetadapter.h; the packaged Qt release build hides it.
        // QVERIFY(QDotNetAdapter::instance().stats().refCount == 1);

        QVERIFY(sb.isValid());
        sb.append("Hello").append(" ");

        StringBuilder sbCpy(sb);

        // Debug-only API in qdotnetadapter.h; the packaged Qt release build hides it.
        // QVERIFY(QDotNetAdapter::instance().stats().refCount == 2);

        QVERIFY(sbCpy.isValid());
        sbCpy.append("World");

        sb = StringBuilder(std::move(sbCpy));

        // Debug-only API in qdotnetadapter.h; the packaged Qt release build hides it.
        // QVERIFY(QDotNetAdapter::instance().stats().refCount == 1);

        sb.append("!");

        QCOMPARE(sb.toString(), "Hello World!");
    }

    void useWrapperClassForUri()
    {
        const Uri uri(QStringLiteral(
            "https://user:password@www.contoso.com:80/Home/Index.htm?q1=v1&q2=v2#FragmentName"));
        const auto segments = uri.segments();

        QCOMPARE(segments.length(), 3);
        QCOMPARE(segments.get(0), "/");
    }

    void handleException()
    {
        StringBuilder stringBuilder(5, 5);
        QString helloWorld;
        try {
            stringBuilder.append("Hello");
            QCOMPARE(stringBuilder.toString(), "Hello");
            stringBuilder.append(" World!");
            helloWorld = stringBuilder.toString();
        } catch (const QDotNetException &ex) {
            helloWorld = ex.type().cast<QDotNetType>().fullName();
        }
        QCOMPARE(helloWorld, "System.ArgumentOutOfRangeException");
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_WrapperTypes)
#include "main.moc"
