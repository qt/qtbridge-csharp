// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QTest>
#include <qdotnettype.h>
#include "QtTestSetupBase.h"

class Test_InboundMemLeak : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    QDotNetFunction<void> fnInboundVoid = nullptr;
    QDotNetFunction<qint32> fnInboundInt32 = nullptr;
    QDotNetFunction<QChar> fnInboundChar = nullptr;
    QDotNetFunction<QString> fnInboundString = nullptr;
    QDotNetFunction<QDateTime> fnInboundDateTime = nullptr;
    QDotNetFunction<QUrl> fnInboundUri = nullptr;
    QDotNetFunction<QDotNetRef> fnInboundObject = nullptr;
    QDotNetFunction<double> fnCorrelation = nullptr;
    QDotNetFunction<double> fnRetainedBytes = nullptr;
    QDotNetFunction<qint32> fnSampleCount = nullptr;
    QDotNetFunction<void, qint32> fnConfigureSampling = nullptr;
    int iterations = 0;

    template <typename TResult>
    void runInbound(QDotNetFunction<TResult> &fnInbound)
    {
        // This is a leak regression test, not a timing benchmark. An explicit loop guarantees
        // the requested call count even when QTest adapts benchmark iterations under load.
        for (int i = 0; i < iterations; ++i)
            fnInbound();
        checkCorrelation();
    }

    void checkCorrelation()
    {
        qint32 sampleCount = fnSampleCount();
        double r = fnCorrelation();
        double retainedBytes = fnRetainedBytes();
        qInfo() << "samples:" << sampleCount << "r:" << r
                << "retained bytes:" << retainedBytes;
        QCOMPARE_GE(sampleCount, 2);

        // Correlation alone is unstable when a short-lived process grows by only a few
        // memory pages. A real instance of QTBRIDGES-330 retains the inbound string buffer
        // on every call, so require both a strong trend and meaningful retained growth.
        constexpr double maxNoiseBytes = 4 * 1024 * 1024;
        QVERIFY2(r < 0.5 || retainedBytes < maxNoiseBytes,
            "Private memory strongly correlates with calls and grew by at least 4 MiB");
    }

private slots:
    void initTestCase()
    {
        bool iterationsOk = false;
        iterations = qEnvironmentVariableIntValue(
            "QTBRIDGE_INBOUND_ITERATIONS", &iterationsOk);
        QVERIFY2(iterationsOk && iterations >= 2,
            "QTBRIDGE_INBOUND_ITERATIONS must be an integer greater than one");

        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
        fnInboundVoid = QDotNetType::staticMethod<void>(TYPE_NAME, "InboundVoid");
        fnInboundInt32 = QDotNetType::staticMethod<qint32>(TYPE_NAME, "InboundInt32");
        fnInboundChar = QDotNetType::staticMethod<QChar>(TYPE_NAME, "InboundChar");
        fnInboundString = QDotNetType::staticMethod<QString>(TYPE_NAME, "InboundString");
        fnInboundDateTime = QDotNetType::staticMethod<QDateTime>(TYPE_NAME, "InboundDateTime");
        fnInboundUri = QDotNetType::staticMethod<QUrl>(TYPE_NAME, "InboundUri");
        fnInboundObject = QDotNetType::staticMethod<QDotNetRef>(TYPE_NAME, "InboundObject");
        fnCorrelation = QDotNetType::staticMethod<double>(TYPE_NAME, "Correlation");
        fnRetainedBytes = QDotNetType::staticMethod<double>(TYPE_NAME, "RetainedBytes");
        fnSampleCount = QDotNetType::staticMethod<qint32>(TYPE_NAME, "SampleCount");
        fnConfigureSampling = QDotNetType::staticMethod<void, qint32>(
            TYPE_NAME, "ConfigureSampling");
        QVERIFY(fnInboundVoid.isValid());
        QVERIFY(fnInboundInt32.isValid());
        QVERIFY(fnInboundChar.isValid());
        QVERIFY(fnInboundString.isValid());
        QVERIFY(fnInboundDateTime.isValid());
        QVERIFY(fnInboundUri.isValid());
        QVERIFY(fnInboundObject.isValid());
        QVERIFY(fnCorrelation.isValid());
        QVERIFY(fnRetainedBytes.isValid());
        QVERIFY(fnSampleCount.isValid());
        QVERIFY(fnConfigureSampling.isValid());
        fnConfigureSampling(iterations);
    }

    void loopInboundVoid()
    {
        runInbound(fnInboundVoid);
    }

    void loopInboundInt32()
    {
        runInbound(fnInboundInt32);
    }

    void loopInboundChar()
    {
        runInbound(fnInboundChar);
    }

    void loopInboundString()
    {
        runInbound(fnInboundString);
    }

    void loopInboundDateTime()
    {
        runInbound(fnInboundDateTime);
    }

    void loopInboundUri()
    {
        runInbound(fnInboundUri);
    }

    void loopInboundObject()
    {
        runInbound(fnInboundObject);
    }

    // Native code also passes strings to the adapter itself, for example the type and method
    // names when it resolves a method. The adapter must free them on every call.
    void loopAdapterStrings()
    {
        const QList<QDotNetParameter> parameters{ QDotNetInbound<void>::Parameter };
        for (int i = 0; i < iterations; ++i) {
            QDotNetFunction<void> fnResolved = QDotNetAdapter::instance().resolveStaticMethod(
                TYPE_NAME, "InboundVoid", parameters);
            fnResolved();
        }
        checkCorrelation();
    }

    void cleanupTestCase() { unloadHost(); }
};

QTEST_MAIN_WITH_DOTNET_SETUP(Test_InboundMemLeak)
#include "main.moc"
