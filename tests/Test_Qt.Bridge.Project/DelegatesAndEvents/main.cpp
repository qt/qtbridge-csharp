// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QSignalSpy>
#include <QTest>
#include <QLocale>

#include <cmath>
#include <limits>

#include <qdotnetdelegate.h>
#include <qdotnetevent.h>
#include <qdotnetobject.h>
#include <qdotnetsafemethod.h>
#include <qdotnetsignal.h>

#include <delegatesandevents/coord3deventargs.h>

#include "object_dispatch.h"
#include "QtTestSetupBase.h"

class PingObserver final : public QObject, public QDotNetEventHandler
{
    Q_OBJECT

public:
signals:
    void pingCompleted(const QString &address, qint64 roundtripMsecs);

private:
    void handleEvent(const QString &eventName, QDotNetObject &, QDotNetObject &eventArgs) override
    {
        if (eventName != "PingCompleted")
            return;
        if (eventArgs.type().fullName() != "DelegatesAndEvents.PingCompletedEventArgs")
            return;

        const auto address = eventArgs.method<QString>("get_Address");
        const auto roundtrip = eventArgs.method<qint64>("get_RoundtripTime");
        emit pingCompleted(address(), roundtrip());
    }
};

class MissionControl final : public QObject, public QDotNetEventHandler
{
    Q_OBJECT

signals:
    void eagleLandedEventArgs(QObject *qEvArgs);

private:
    void handleEvent(const QString &name, QDotNetObject &sender, QDotNetObject &args) override
    {
        Q_UNUSED(name);
        Q_UNUSED(sender);

        if (!args.type().isAssignableTo<QDotNetEventArgs>())
            return;

        QObject *qEvArgs = QtDotNet::objectDispatch(args);
        if (!qEvArgs)
            return;

        qEvArgs->setParent(this);
        emit eagleLandedEventArgs(qEvArgs);
    }
};

class LegacyMissionControl final : public QObject, public QDotNetEventHandler
{
    Q_OBJECT

signals:
    void eagleLanded();
    void theEagleHasLanded();
    void theEagleHasLanded_WRONG_PARAMS(const QString &param1, const QString &param2);
    void theEagleHasLanded_WRONG_ORDER(const QString &param1, const QString &param2);
    void theEagleHasLanded_OK(const QString &param1, const QString &param2);

private:
    void handleEvent(const QString &name, QDotNetObject &sender, QDotNetObject &args) override
    {
        if (!args.type().isAssignableTo<QDotNetEventArgs>())
            return;

        auto eventArgs = args.cast<QDotNetEventArgs>();
        if (!eventArgs.isValid())
            return;

        auto eventSignals = QDotNetSignal::fromEvent(name, sender);
        for (auto &eventSignal : eventSignals) {
            if (!QDotNetSignal::convert(eventSignal, sender, eventArgs))
                continue;

            const auto signalName = eventSignal.name();
            if (signalName == "EagleLanded") {
                emit eagleLanded();
            } else if (signalName == "TheEagleHasLanded") {
                emit theEagleHasLanded();
            } else if (signalName == "TheEagleHasLanded_WRONG_PARAMS") {
                emit theEagleHasLanded_WRONG_PARAMS(
                    eventSignal.arg<QString>(0), eventSignal.arg<QString>(1));
            } else if (signalName == "TheEagleHasLanded_WRONG_ORDER") {
                emit theEagleHasLanded_WRONG_ORDER(
                    eventSignal.arg<QString>(0), eventSignal.arg<QString>(1));
            } else if (signalName == "TheEagleHasLanded_OK") {
                emit theEagleHasLanded_OK(
                    eventSignal.arg<QString>(0), eventSignal.arg<QString>(1));
            }
        }
    }
};

class ApolloXI
{
public:
    static void setAssemblyName(const QString &assemblyName)
    {
        AssemblyName = assemblyName;
    }

    ApolloXI()
    {
        const auto typeName = QString("%1, %2").arg("DelegatesAndEvents.Apollo11", AssemblyName);
        const auto newApollo = QDotNetObject::constructor(typeName);
        object = newApollo();
    }

    void subscribe(const QString &eventName, QDotNetEventHandler *handler)
    {
        object.subscribe(eventName, handler);
    }

    void unsubscribe(const QString &eventName, QDotNetEventHandler *handler)
    {
        object.unsubscribe(eventName, handler);
    }

    void land(double x, double y, double z)
    {
        object.method("Land", landMethod).invoke(object, x, y, z);
    }

private:
    static inline QString AssemblyName;
    QDotNetObject object;
    QDotNetSafeMethod<void, double, double, double> landMethod;
};

class Test_DelegatesAndEvents : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    [[nodiscard]] QString delegateExportsTypeName() const
    {
        return QString("%1, %2").arg("DelegatesAndEvents.DelegateExports", assemblyName);
    }

    [[nodiscard]] QString pingEmitterTypeName() const
    {
        return QString("%1, %2").arg("DelegatesAndEvents.PingEmitter", assemblyName);
    }

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        ApolloXI::setAssemblyName(assemblyName);
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
    }

    void delegates()
    {
        auto plus42 = QtDotNet::call<QDotNetDelegate<int, int>>(
            delegateExportsTypeName(),
            "get_Plus42");
        QCOMPARE(plus42(3), 45);
    }

    void emitSignalFromEvent()
    {
        PingObserver observer;
        const auto newPing = QDotNetObject::constructor(pingEmitterTypeName());
        auto ping = newPing();
        ping.subscribe("PingCompleted", &observer);
        QDotNetSafeMethod<void, QString> sendAsync;
        int signalCount = 0;

        connect(&observer, &PingObserver::pingCompleted,
            [&signalCount](const QString &address, qint64 roundtripMsecs)
            {
                QVERIFY(!address.isEmpty());
                QVERIFY(roundtripMsecs >= 0);
                ++signalCount;
            });

        ping.method("SendAsync", sendAsync).invoke(ping, "127.0.0.1");
        ping.method("SendAsync", sendAsync).invoke(ping, "localhost");
        ping.method("SendAsync", sendAsync).invoke(ping, "qt.io");
        ping.method("SendAsync", sendAsync).invoke(ping, "bridge");

        QCOMPARE(signalCount, 4);
    }

    void signalConverters()
    {
        // Current behavior after 4bcc01ec6c4b15cd429874f6f54a66f8817061d2:
        // One Qt signal carrying a QObject* wrapper for the managed EventArgs.
        MissionControl houston;
        ApolloXI eagle;
        eagle.subscribe("EagleLanded", &houston);

        const QSignalSpy spyEagleLanded(&houston, &MissionControl::eagleLandedEventArgs);

        eagle.land(23.433333, 0.6875, 30.0);
        eagle.unsubscribe("EagleLanded", &houston);

        QVERIFY(!spyEagleLanded.isEmpty());
        QCOMPARE(spyEagleLanded.first().count(), 1);
        QObject *payload = spyEagleLanded.first().at(0).value<QObject *>();
        QVERIFY(payload != nullptr);

        auto *coords = qobject_cast<DelegatesAndEvents::Coord3DEventArgs *>(payload);
        QVERIFY(coords != nullptr);
        QCOMPARE(coords->x(), 23.433333);
        QCOMPARE(coords->y(), 0.6875);
        QCOMPARE(coords->z(), 30.0);
    }

    void legacySignalConverters()
    {
        const auto localize = [](const QVariant &value) -> double {
            bool ok = false;
            const QString text = value.toString();
            double parsed = QLocale::c().toDouble(text, &ok);
            if (!ok)
                parsed = QLocale().toDouble(text, &ok);
            if (!ok)
                return std::numeric_limits<double>::quiet_NaN();
            return parsed;
        };

        LegacyMissionControl houston;
        ApolloXI eagle;
        eagle.subscribe("EagleLanded", &houston);

        const QSignalSpy spyEagleLanded(&houston, &LegacyMissionControl::eagleLanded);
        const QSignalSpy spyTheEagleHasLanded(&houston,
            &LegacyMissionControl::theEagleHasLanded);
        const QSignalSpy spyWrongParams(&houston,
            &LegacyMissionControl::theEagleHasLanded_WRONG_PARAMS);
        const QSignalSpy spyWrongOrder(&houston,
            &LegacyMissionControl::theEagleHasLanded_WRONG_ORDER);
        const QSignalSpy spyOk(&houston, &LegacyMissionControl::theEagleHasLanded_OK);

        eagle.land(23.433333, 0.6875, 30.0);
        eagle.unsubscribe("EagleLanded", &houston);

        QVERIFY(!spyEagleLanded.isEmpty());
        QVERIFY(spyEagleLanded.first().isEmpty());

        QVERIFY(!spyTheEagleHasLanded.isEmpty());
        QVERIFY(spyTheEagleHasLanded.first().isEmpty());

        QVERIFY(!spyWrongParams.isEmpty());
        QCOMPARE(spyWrongParams.first().count(), 2);
        QCOMPARE(spyWrongParams.first().at(0).toString(), "30");
        const double wrongParamsValue = localize(spyWrongParams.first().at(1));
        QVERIFY(!std::isnan(wrongParamsValue));
        QCOMPARE(wrongParamsValue, 23.433333);

        QVERIFY(!spyWrongOrder.isEmpty());
        QCOMPARE(spyWrongOrder.first().count(), 2);
        const double wrongOrderX = localize(spyWrongOrder.first().at(0));
        const double wrongOrderY = localize(spyWrongOrder.first().at(1));
        QVERIFY(!std::isnan(wrongOrderX));
        QVERIFY(!std::isnan(wrongOrderY));
        QCOMPARE(wrongOrderX, 23.433333);
        QCOMPARE(wrongOrderY, 0.6875);

        QVERIFY(!spyOk.isEmpty());
        QCOMPARE(spyOk.first().count(), 2);
        QCOMPARE(spyOk.first().at(0).toString(), QString::fromUtf8("0° 41' 15'' N"));
        QCOMPARE(spyOk.first().at(1).toString(), QString::fromUtf8("23° 25' 59'' E"));
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_DelegatesAndEvents)
#include "main.moc"
