/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include "foo.h"
#include "stringbuilder.h"
#include "uri.h"

#include <qdotnetadapter.h>
#include <qdotnetarray.h>
#include <qdotnetcallback.h>
#include <qdotnethost.h>
#include <qdotnetmarshal.h>
#include <qdotnetobject.h>
#include <qdotnetsafemethod.h>
#include <qdotnettype.h>
#include <qdotnetstatic.h>
#include <qdotnetdelegate.h>
#include <qdotnetsignal.h>

#include <iqvariant.h>
#include <iqmodelindex.h>

#include <qdotnetabstractlistmodel.h>

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QChar>
#include <QDebug>
#include <QDir>
#include <QElapsedTimer>
#include <QList>
#include <QMap>
#include <QObject>
#include <QSignalSpy>
#include <QString>
#include <QStringListModel>
#include <QThread>

#include <QtTest>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#define TESTCASE_DOTNET_MAIN
//#define TESTCASE_QT_MAIN

#if defined(TESTCASE_DOTNET_MAIN)
    #define TEST_APP_STARTUP
    #define TEST_FUNCTION_CALLS
    #define TEST_APP_SHUTDOWN
    #define TEST_HOST_UNLOAD
#elif defined(TESTCASE_QT_MAIN)
    #define TEST_HOST_LOAD
    #define TEST_FUNCTION_CALLS
    #define TEST_HOST_UNLOAD
#endif

//#define COREHOST_TRACE

class tst_qtdotnet : public QObject
{
    Q_OBJECT

public:
    tst_qtdotnet() = default;

private:
    int refCount = 0;
    bool skipCleanup = false;

private slots:

    void initTestCase()
    {
#ifdef COREHOST_TRACE
        qputenv("COREHOST_TRACE", "1");
#endif
    }

    void init()
    {
        if (!QDotNetAdapter::instance().isValid())
            return;
        refCount = QDotNetAdapter::instance().stats().refCount;
    }

    void cleanup()
    {
        if (skipCleanup) {
            skipCleanup = false;
            QSKIP("cleanup skipped");
        }
        if (!QDotNetAdapter::instance().isValid())
            return;
        QVERIFY(QDotNetAdapter::instance().stats().refCount == refCount);
    }

#ifdef TEST_APP_STARTUP
    void appStartup();
#endif //TEST_APP_STARTUP
#ifdef TEST_HOST_LOAD
    void loadHost();
    void runtimeProperties();
#endif //TEST_HOST_LOAD
#ifdef TEST_FUNCTION_CALLS
    void resolveFunction();
    void callFunction();
    void callFunctionWithCustomMarshaling();
    void callDefaultEntryPoint();
    void callWithComplexArg();
#endif //TEST_FUNCTION_CALLS
#ifdef TEST_HOST_LOAD
    void adapterInit();
#endif //TEST_HOST_LOAD
#ifdef TEST_FUNCTION_CALLS
    void callStaticMethod();
    void handleException();
    void createObject();
    void callInstanceMethod();
    void useWrapperClassForStringBuilder();
    void useWrapperClassForUri();
    void emitSignalFromEvent();
    void propertyBinding();
    void implementInterface();
    void arrayOfInts();
    void arrayOfStrings();
    void arrayOfObjects();
    void variantNull();
    void variantGet();
    void variantSet();
    void modelIndexNull();
    void modelIndexGet();
    void models();
    void delegates();
    void signalConverters();
    void fieldAccess();
#endif //TEST_FUNCTION_CALLS
#ifdef TEST_APP_SHUTDOWN
    void appShutdown();
#endif //TEST_APP_SHUTDOWN
#ifdef TEST_HOST_UNLOAD
    void unloadHost();
#endif //TEST_HOST_UNLOAD
};

QDotNetHost dotNetHost;
QThread *dotnetThread = nullptr;

#ifdef TEST_APP_STARTUP

void tst_qtdotnet::appStartup()
{
    dotnetThread = QThread::create(
        [this]()
        {
            dotNetHost.loadApp(
                QDir(QCoreApplication::applicationDirPath()).filePath("FooConsoleApp.dll"));
            dotNetHost.runApp();
        });
    dotnetThread->start();
    bool block = true;
    while (!dotNetHost.isReady())
        QThread::sleep(1);
    QDotNetAdapter::instance().init(
        QDir(QCoreApplication::applicationDirPath()).filePath("Qt.DotNet.Adapter.dll"),
        "Qt.DotNet.Adapter", "Qt.DotNet.Adapter", &dotNetHost);
}
#endif //TEST_APP_STARTUP

#ifdef TEST_APP_STARTUP
void tst_qtdotnet::appShutdown()
{
    if (!dotnetThread)
        QSKIP("App thread not running");

    QtDotNet::call<void, bool>("FooConsoleApp.Program, FooConsoleApp", "set_KeepRunning", false);
    QThread::sleep(1);
    while (dotnetThread->isRunning()) {
        qInfo() << "App thread still running...";
        QThread::sleep(1);
    }
}
#endif //TEST_APP_STARTUP

#ifdef TEST_HOST_LOAD
void tst_qtdotnet::loadHost()
{
    QVERIFY(!dotNetHost.isLoaded());
    QVERIFY(dotNetHost.load());
    QVERIFY(dotNetHost.isLoaded());
}

void tst_qtdotnet::runtimeProperties()
{
    QVERIFY(dotNetHost.isLoaded());
    QMap<QString, QString> runtimeProperties = dotNetHost.runtimeProperties();
    QVERIFY(!runtimeProperties.isEmpty());
    for (auto prop = runtimeProperties.constBegin(); prop != runtimeProperties.constEnd(); ++prop) {
        qInfo() << prop.key() << "=" << QString("%1%2")
            .arg(prop.value().left(100)).arg(prop.value().length() > 100 ? "..." : "");
    }
}

void tst_qtdotnet::adapterInit()
{
    QVERIFY(!QDotNetAdapter::instance().isValid());
    QDotNetAdapter::instance().init(
        QDir(QCoreApplication::applicationDirPath()).filePath("Qt.DotNet.Adapter.dll"),
        "Qt.DotNet.Adapter", "Qt.DotNet.Adapter", &dotNetHost);
    QVERIFY(QDotNetAdapter::instance().isValid());
}
#endif //TEST_HOST_LOAD

#ifdef TEST_FUNCTION_CALLS
QDotNetFunction<QString, QString, int> formatNumber;

void tst_qtdotnet::resolveFunction()
{
    QVERIFY(dotNetHost.isLoaded());
    QVERIFY(!formatNumber.isValid());
    QVERIFY(dotNetHost.resolveFunction(formatNumber,
        QDir(QCoreApplication::applicationDirPath()).filePath("FooLib.dll"),
        Foo::AssemblyQualifiedName, "FormatNumber", "FooLib.Foo+FormatNumberDelegate, FooLib"));

    QVERIFY(formatNumber.isValid());
}

void tst_qtdotnet::callFunction()
{
    QVERIFY(dotNetHost.isLoaded());
    QVERIFY(formatNumber.isValid());

    const QString formattedText = formatNumber("[{0}]", 42);

    QCOMPARE(formattedText, "[42]");
}

struct DoubleAsInt {};

template<>
struct QDotNetOutbound<DoubleAsInt>
{
    using SourceType = double;
    using OutboundType = int;
    static OutboundType convert(SourceType arg)
    {
        return qRound(arg);
    }
};

struct QUpperCaseString
{};

template<>
struct QDotNetNull<QUpperCaseString>
{
    static QString value() { return {}; }
    static bool isNull(const QString& s) { return  s.isNull() || s.isEmpty(); }
};

template<>
struct QDotNetInbound<QUpperCaseString>
{
    using InboundType = QChar*;
    using TargetType = QString;
    static TargetType convert(InboundType inboundValue)
    {
        return QString(inboundValue).toUpper();
    }
};

void tst_qtdotnet::callFunctionWithCustomMarshaling()
{
    QVERIFY(dotNetHost.isLoaded());

    QDotNetFunction<QUpperCaseString, QString, DoubleAsInt> formatDouble;
    QVERIFY(dotNetHost.resolveFunction(formatDouble,
        QDir(QCoreApplication::applicationDirPath()).filePath("FooLib.dll"),
        Foo::AssemblyQualifiedName, "FormatNumber", "FooLib.Foo+FormatNumberDelegate, FooLib"));

    QVERIFY(formatDouble.isValid());

    const QString formattedText = formatDouble("result = [{0}]", 41.5);

    QCOMPARE(formattedText, "RESULT = [42]");
}

void tst_qtdotnet::callDefaultEntryPoint()
{
    QVERIFY(dotNetHost.isLoaded());

    QDotNetFunction<quint32, void*, qint32> entryPoint;
    QVERIFY(dotNetHost.resolveFunction(entryPoint,
        QDir(QCoreApplication::applicationDirPath()).filePath("FooLib.dll"),
        Foo::AssemblyQualifiedName, "EntryPoint"));

    QVERIFY(entryPoint.isValid());

    QString fortyTwo("42");
    const qint32 returnValue = entryPoint(fortyTwo.data(), static_cast<qint32>(fortyTwo.length()));

    QCOMPARE(returnValue, 42);
}

struct Date
{
    QString year;
    QString month;
    QString day;
};

struct DateOutbound
{
    const QChar* year;
    const QChar* month;
    const QChar* day;
};

template<>
struct QDotNetOutbound<Date>
{
    using SourceType = const Date&;
    using OutboundType = const DateOutbound;
    static DateOutbound convert(SourceType arg)
    {
        return { arg.year.data(), arg.month.data(), arg.day.data() };
    }
};

void tst_qtdotnet::callWithComplexArg()
{
    QVERIFY(dotNetHost.isLoaded());
    QDotNetFunction<QString, QString, Date> formatDate;
    QVERIFY(dotNetHost.resolveFunction(formatDate,
        QDir(QCoreApplication::applicationDirPath()).filePath("FooLib.dll"),
        Foo::AssemblyQualifiedName, "FormatDate", "FooLib.Foo+FormatDateDelegate, FooLib"));

    QVERIFY(formatDate.isValid());

    const Date xmas{ "2022", "12", "25" };
    const QString formattedText = formatDate("Today is {0}-{1}-{2}", xmas);

    QCOMPARE(formattedText, "Today is 2022-12-25");
}

void tst_qtdotnet::callStaticMethod()
{
    const QDotNetType environment = QDotNetType::typeOf("System.Environment");
    const auto getEnvironmentVariable
        = environment.staticMethod<QString, QString>("GetEnvironmentVariable");
    const QString path = getEnvironmentVariable("PATH");
    QVERIFY(path.length() > 0);
    const QString samePath = QtDotNet::call<QString, QString>(
        "System.Environment", "GetEnvironmentVariable", "PATH");
    QVERIFY(path == samePath);
}

void tst_qtdotnet::createObject()
{
    const auto newStringBuilder = QDotNetObject::constructor("System.Text.StringBuilder");
    QDotNetObject stringBuilder = newStringBuilder();
    QVERIFY(QDotNetAdapter::instance().stats().refCount == 1);
}

void tst_qtdotnet::callInstanceMethod()
{
    const auto newStringBuilder = QDotNetObject::constructor("System.Text.StringBuilder");
    const auto stringBuilder = newStringBuilder();
    const auto append = stringBuilder.method<QDotNetObject, QString>("Append");
    std::ignore = append("Hello");
    std::ignore = append(" World!");
    const QString helloWorld = stringBuilder.toString();
    QVERIFY(helloWorld == "Hello World!");
}

void tst_qtdotnet::useWrapperClassForStringBuilder()
{
    StringBuilder sb;
    QVERIFY(QDotNetAdapter::instance().stats().refCount == 1);
    QVERIFY(sb.isValid());
    sb.append("Hello").append(" ");
    StringBuilder sbCpy(sb);
    QVERIFY(QDotNetAdapter::instance().stats().refCount == 2);
    QVERIFY(sbCpy.isValid());
    sbCpy.append("World");
    sb = StringBuilder(std::move(sbCpy));
    QVERIFY(QDotNetAdapter::instance().stats().refCount == 1);
    sb.append("!");
    QCOMPARE(sb.toString(), "Hello World!");
}

void tst_qtdotnet::useWrapperClassForUri()
{
    const Uri uri(QStringLiteral(
        "https://user:password@www.contoso.com:80/Home/Index.htm?q1=v1&q2=v2#FragmentName"));
    QVERIFY(uri.segments().length() == 3);
    QVERIFY(uri.segments()[0]->compare("/") == 0);
}

void tst_qtdotnet::handleException()
{
    StringBuilder stringBuilder(5, 5);
    QString helloWorld;
    try {
        stringBuilder.append("Hello");
        QVERIFY(stringBuilder.toString() == "Hello");
        stringBuilder.append(" World!");
        helloWorld = stringBuilder.toString();
    }
    catch (const QDotNetException &ex) {
        helloWorld = ex.type().cast<QDotNetType>().fullName();
    }
    QVERIFY(helloWorld == "System.ArgumentOutOfRangeException");
}

class Ping final : public QObject, public QDotNetObject, public QDotNetEventHandler
{
    Q_OBJECT

public:
    Q_DOTNET_OBJECT_INLINE(Ping, "System.Net.NetworkInformation.Ping, System", );

    Ping()
        : QDotNetObject(QDotNetSafeMethod(constructor<Ping>()).invoke(nullptr))
    {
        subscribe("PingCompleted", this);
    }
    ~Ping() override = default;

    void sendAsync(const QString& hostNameOrAddress)
    {
        method("SendAsync", safeSendAsync).invoke(*this, hostNameOrAddress, nullptr);
    }

    void sendAsyncCancel()
    {
        method("SendAsyncCancel", safeSendAsyncCancel).invoke(*this);
    }

signals:
    void pingCompleted(const QString& address, qint64 roundtripMsecs);
    void pingError();

private:
    void handleEvent(const QString& evName, QDotNetObject& evSrc, QDotNetObject& evArgs) override
    {
        if (evName != "PingCompleted")
            return;
        if (evArgs.type().fullName() != "System.Net.NetworkInformation.PingCompletedEventArgs")
            return;
        const auto getReply = evArgs.method<QDotNetObject>("get_Reply");
        const auto reply = getReply();
        if (reply.isValid()) {
            const auto replyAddress = reply.method<QDotNetObject>("get_Address");
            const auto replyRoundtrip = reply.method<qint64>("get_RoundtripTime");
            emit pingCompleted(replyAddress().toString(), replyRoundtrip());
        }
        else {
            emit pingError();
        }
    }
    QDotNetSafeMethod<void, QString, QtDotNet::Null> safeSendAsync;
    QDotNetSafeMethod<void> safeSendAsyncCancel;
};

void tst_qtdotnet::emitSignalFromEvent()
{
    Ping ping;
    bool waiting = true;
    int signalCount = 0;
    connect(&ping, &Ping::pingCompleted,
        [&waiting, &signalCount](const QString& address, qint64 roundtripMsecs) {
            qInfo() << "Reply from" << address << "in" << roundtripMsecs << "msecs";
    signalCount++;
    waiting = false;
        });
    connect(&ping, &Ping::pingError,
        [&waiting, &signalCount] {
            qInfo() << "Ping error";
    signalCount++;
    waiting = false;
        });
    qInfo() << "Pinging www.qt.io:";
    QElapsedTimer waitTime;
    for (int i = 0; i < 4; ++i) {
        waitTime.restart();
        waiting = true;
        ping.sendAsync("www.qt.io");
        while (waiting) {
            QCoreApplication::processEvents();
            if (waitTime.elapsed() > 3000) {
                ping.sendAsyncCancel();
                waiting = false;
                qInfo() << "Ping timeout";
            }
        }
    }
    QVERIFY(signalCount == 4);
}

void tst_qtdotnet::propertyBinding()
{
    Foo foo;
    const QSignalSpy spy(&foo, &Foo::barChanged);
    for (int i = 0; i < 1000; ++i)
        foo.setBar(QString("hello x %1").arg(i + 1));
    QVERIFY(foo.bar() == "hello x 1000");
    QVERIFY(spy.count() == 1000);
}

struct ToUpper : IBarTransformation
{
    Uri uri = Uri("https://qt.io/");
    QString transform(const QString& bar) override
    {
        return bar.toUpper();
    }
    Uri getUri(int n) override
    {
        return uri;
    }
    void setUri(const Uri &uri) override
    {
        this->uri = uri;
    }
    int getNumber() override
    {
        return 42;
    }
};

void tst_qtdotnet::implementInterface()
{
    const ToUpper transfToUpper;
    Foo foo(transfToUpper);
    foo.setBar("hello there");
    QVERIFY(foo.bar() == "HELLO THERE (https://qt.io/developers)");
}

void tst_qtdotnet::arrayOfInts()
{
    QDotNetArray<qint32> a(11);
    a[0] = 0;
    a[1] = 1;
    for (int i = 2; i < a.length(); ++i)
        a[i] = a[i - 1] + a[i - 2];
    QVERIFY(a[10] == 55);
}

void tst_qtdotnet::arrayOfStrings()
{
    QDotNetArray<QString> a(8);
    a[0] = "Lorem";
    a[1] = "ipsum";
    a[2] = "dolor";
    a[3] = "sit";
    a[4] = "amet,";
    a[5] = "consectetur";
    a[6] = "adipiscing";
    a[7] = "elit.";
    const auto stringType = QDotNetType::typeOf("System.String");
    const auto join = stringType.staticMethod<QString, QString, QDotNetArray<QString>>("Join");
    const auto loremIpsum = join(" ", a);
    QVERIFY(loremIpsum == "Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
}

void tst_qtdotnet::arrayOfObjects()
{
    QDotNetArray<StringBuilder> a(8);
    for (int i = 0; i < a.length(); ++i)
        a[i] = StringBuilder();
    a[0]->append("Lorem");
    a[1]->append(a[0]->toString()).append(" ipsum");
    a[2]->append(a[1]->toString()).append(" dolor");
    a[3]->append(a[2]->toString()).append(" sit");
    a[4]->append(a[3]->toString()).append(" amet,");
    a[5]->append(a[4]->toString()).append(" consectetur");
    a[6]->append(a[5]->toString()).append(" adipiscing");
    a[7]->append(a[6]->toString()).append(" elit.");
    QVERIFY(a[7]->toString() == "Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
}

void tst_qtdotnet::variantNull()
{
    auto getVariant = QDotNetType::staticMethod<IQVariant>("FooLib.Foo, FooLib", "GetVariant");
    auto iqv = getVariant();
    auto &qv = *iqv.dataAs<QVariant>();
    QVERIFY(!qv.isValid());
}

void tst_qtdotnet::variantGet()
{
    auto getVariant = QDotNetType::staticMethod<IQVariant, QString>("FooLib.Foo, FooLib", "GetVariant");
    auto iqv = getVariant("foobar");
    auto &qv = *iqv.dataAs<QVariant>();
    QVERIFY(qv.toString() == "foobar");
}

void tst_qtdotnet::variantSet()
{
    QVariant qv = "foobar";
    IQVariant iqv(qv);
    auto toUpper = QDotNetType::staticMethod<void, IQVariant>("FooLib.Foo, FooLib", "VariantStringToUpper");
    toUpper(iqv);
    QVERIFY(qv.toString() == "FOOBAR");
}

struct TestModel : public QStringListModel
{
    QModelIndex getIndex(int row, int col, void *ptr)
    {
        return createIndex(row, col, ptr);
    }
};

void tst_qtdotnet::modelIndexNull()
{
    auto getModelIndex = QDotNetType::staticMethod<IQModelIndex>("FooLib.Foo, FooLib", "GetModelIndex");
    auto iqmi = getModelIndex();
    auto &qmi = *iqmi.dataAs<QModelIndex>();
    QVERIFY(!qmi.isValid());
}

void tst_qtdotnet::modelIndexGet()
{
    TestModel tm;
    auto idx = IQModelIndex(tm.getIndex(2, 3, reinterpret_cast<void *>(7)));
    auto idxRowColPtr = QDotNetType::staticMethod<int, IQModelIndex>("FooLib.Foo, FooLib", "ModelIndexRowColPtr");
    auto rcp = idxRowColPtr(idx);
    QVERIFY(rcp == 42);
}

struct TestListModel : public QDotNetObject
{
    Q_DOTNET_OBJECT_INLINE(TestListModel, "FooLib.Foo+TestListModel, FooLib");
    TestListModel()
        : QDotNetObject(constructor<TestListModel>().invoke(nullptr))
    { }
    QAbstractListModel *base() const
    {
        auto baseObj = method("get_Base", fnBase).invoke(*this);
        auto baseInterface = baseObj.cast<QDotNetInterface>();
        return baseInterface.dataAs<QAbstractListModel>();
    }
    mutable QDotNetFunction<QDotNetRef> fnBase = nullptr;
};

void tst_qtdotnet::models()
{
    const auto testModel = TestListModel();
    auto *baseModel = testModel.base();
    auto n = baseModel->rowCount();
    QVERIFY(n == 2);
    auto ff = baseModel->flags(baseModel->index(0));
    QVERIFY(ff == (Qt::ItemIsSelectable | Qt::ItemIsEnabled | Qt::ItemNeverHasChildren));
    auto it0 = baseModel->data(baseModel->index(0));
    QVERIFY(it0.toString() == "FOO");
    auto it1 = baseModel->data(baseModel->index(1));
    QVERIFY(it1.toString() == "BAR");
    skipCleanup = true; // TODO: figure out why refs are still pending here
}

void tst_qtdotnet::delegates()
{
    auto plus42 = QtDotNet::call<QDotNetDelegate<int, int>>("FooLib.Foo, FooLib", "get_Plus42");
    QVERIFY(plus42(3) == 45);
}

class ApolloXI : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT_INLINE(ApolloXI, "FooLib.Apollo11, FooLib");

    ApolloXI()
        : QDotNetObject(QDotNetSafeMethod(constructor<ApolloXI>()).invoke(nullptr))
    {
    }
    ~ApolloXI() override = default;

    void land(double x, double y, double z)
    {
        return method<void, double, double, double>("Land").invoke(*this, x, y, z);
    }
};

class MissionControl final : public QObject, public QDotNetEventHandler
{
    Q_OBJECT
signals:
    void eagleLanded();
    void theEagleHasLanded();
    void theEagleHasLanded_WRONG_PARAMS(const QString&, const QString&);
    void theEagleHasLanded_WRONG_ORDER(const QString&, const QString&);
    void theEagleHasLanded_OK(const QString&, const QString&);

private:
    void handleEvent(const QString& name, QDotNetObject& sender, QDotNetObject& args) override
    {
        if (!args.type().isAssignableTo<QDotNetEventArgs>())
            return;
        auto eventArgs = args.cast<QDotNetEventArgs>();
        if (!eventArgs.isValid())
            return;

        auto eventSignals = QDotNetSignal::fromEvent(name, sender);

        for (auto& eventSignal : eventSignals) {
            if (!QDotNetSignal::convert(eventSignal, sender, eventArgs))
                continue;
            auto signalName = eventSignal.name();
            if (signalName == "EagleLanded") {
                emit eagleLanded();
            } else if (signalName == "TheEagleHasLanded") {
                emit theEagleHasLanded();
            } else if (signalName == "TheEagleHasLanded_WRONG_PARAMS") {
                emit theEagleHasLanded_WRONG_PARAMS(eventSignal.arg<QString>(0), eventSignal.arg<QString>(1));
            } else if (signalName == "TheEagleHasLanded_WRONG_ORDER") {
                emit theEagleHasLanded_WRONG_ORDER(eventSignal.arg<QString>(0), eventSignal.arg<QString>(1));
            } else if (signalName == "TheEagleHasLanded_OK") {
                emit theEagleHasLanded_OK(eventSignal.arg<QString>(0), eventSignal.arg<QString>(1));
            }
        }
    }
};

void tst_qtdotnet::signalConverters()
{
    MissionControl houston;
    ApolloXI eagle;
    eagle.subscribe("EagleLanded", &houston);

    const QSignalSpy spyEagleLanded(&houston, &MissionControl::eagleLanded);
    const QSignalSpy spyTheEagleHasLanded(&houston, &MissionControl::theEagleHasLanded);
    const QSignalSpy spyTheEagleHasLanded_WRONG_PARAMS(&houston, &MissionControl::theEagleHasLanded_WRONG_PARAMS);
    const QSignalSpy spyTheEagleHasLanded_WRONG_ORDER(&houston, &MissionControl::theEagleHasLanded_WRONG_ORDER);
    const QSignalSpy spyTheEagleHasLanded_OK(&houston, &MissionControl::theEagleHasLanded_OK);

    eagle.land(23.433333, 0.6875, 30);
    eagle.unsubscribe("EagleLanded", &houston);

    QVERIFY(!spyEagleLanded.isEmpty());
    QVERIFY(spyEagleLanded.first().isEmpty());

    QVERIFY(!spyTheEagleHasLanded.isEmpty());
    QVERIFY(spyTheEagleHasLanded.first().isEmpty());

    QVERIFY(!spyTheEagleHasLanded_WRONG_PARAMS.isEmpty());
    QVERIFY(spyTheEagleHasLanded_WRONG_PARAMS.first().count() == 2);
    QVERIFY(spyTheEagleHasLanded_WRONG_PARAMS.first().at(0) == "30");
    QVERIFY(spyTheEagleHasLanded_WRONG_PARAMS.first().at(1) == "23.433333");

    QVERIFY(!spyTheEagleHasLanded_WRONG_ORDER.isEmpty());
    QVERIFY(spyTheEagleHasLanded_WRONG_ORDER.first().count() == 2);
    QVERIFY(spyTheEagleHasLanded_WRONG_ORDER.first().at(0) == "23.433333");
    QVERIFY(spyTheEagleHasLanded_WRONG_ORDER.first().at(1) == "0.6875");

    QVERIFY(!spyTheEagleHasLanded_OK.isEmpty());
    QVERIFY(spyTheEagleHasLanded_OK.first().count() == 2);
    QVERIFY(spyTheEagleHasLanded_OK.first().at(0) == "0° 41' 15'' N");
    QVERIFY(spyTheEagleHasLanded_OK.first().at(1) == "23° 25' 59'' E");

    skipCleanup = true; // TODO: figure out why refs are still pending here
}

void tst_qtdotnet::fieldAccess()
{
    // Value-type constant
    QVERIFY(Foo::fooNumberConst() == 42);

    // Ref-type constant
    QVERIFY(Foo::fooStringConst() == "FOO");

    // Static field
    QVERIFY(Foo::fooStaticField() == -42);
    Foo::setFooStaticField(123);
    QVERIFY(Foo::fooStaticField() == 123);

    // Instance field
    Foo foo;
    QVERIFY(foo.fooField() == 42);
    foo.setFooField(123);
    QVERIFY(foo.fooField() == 123);
}

#endif //TEST_FUNCTION_CALLS

#ifdef TEST_HOST_UNLOAD
void tst_qtdotnet::unloadHost()
{
    QVERIFY(dotNetHost.isLoaded());

    dotNetHost.unload();

    QVERIFY(!dotNetHost.isLoaded());
}
#endif //TEST_HOST_UNLOAD

QTEST_MAIN(tst_qtdotnet)
#include "tst_qtdotnet.moc"
