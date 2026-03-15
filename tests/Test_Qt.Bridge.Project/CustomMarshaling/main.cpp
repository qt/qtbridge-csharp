// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QChar>
#include <QDebug>
#include <QTest>

#include <qdotnethost.h>
#include <qdotnetmarshal.h>

#include "QtTestSetupBase.h"

struct DoubleAsInt
{
};

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
{
};

template<>
struct QDotNetNull<QUpperCaseString>
{
    static QString value() { return {}; }
    static bool isNull(const QString &s) { return s.isNull() || s.isEmpty(); }
};

template<>
struct QDotNetInbound<QUpperCaseString>
{
    using InboundType = QChar *;
    using TargetType = QString;

    static TargetType convert(InboundType inboundValue)
    {
        return QString(inboundValue).toUpper();
    }
};

struct Date
{
    QString year;
    QString month;
    QString day;
};

struct DateOutbound
{
    const QChar *year;
    const QChar *month;
    const QChar *day;
};

template<>
struct QDotNetOutbound<Date>
{
    using SourceType = const Date &;
    using OutboundType = const DateOutbound *;

    static OutboundType convert(SourceType arg)
    {
        thread_local DateOutbound outbound;
        outbound = { arg.year.data(), arg.month.data(), arg.day.data() };
        return &outbound;
    }
};

class Test_CustomMarshaling : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    [[nodiscard]] QString exportsTypeName() const
    {
        return QString("%1, %2").arg("CustomMarshaling.FunctionExports", assemblyName);
    }

    [[nodiscard]] QString formatNumberDelegateTypeName() const
    {
        return QString("%1, %2")
            .arg("CustomMarshaling.FunctionExports+FormatNumberDelegate", assemblyName);
    }

    [[nodiscard]] QString formatDateDelegateTypeName() const
    {
        return QString("%1, %2")
            .arg("CustomMarshaling.FunctionExports+FormatDateDelegate", assemblyName);
    }

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
    }

    void callFunctionWithCustomMarshaling()
    {
        QDotNetFunction<QUpperCaseString, QString, DoubleAsInt> formatDouble;
        QVERIFY(dotNetHost->resolveFunction(formatDouble,
            assemblyPath,
            exportsTypeName(),
            "FormatNumber",
            formatNumberDelegateTypeName()));

        QVERIFY(formatDouble.isValid());

        const QString formattedText = formatDouble("result = [{0}]", 41.5);
        QCOMPARE(formattedText, "RESULT = [42]");
    }

    void callWithComplexArg()
    {
        QDotNetFunction<QString, QString, Date> formatDate;
        QVERIFY(dotNetHost->resolveFunction(formatDate,
            assemblyPath,
            exportsTypeName(),
            "FormatDate",
            formatDateDelegateTypeName()));

        QVERIFY(formatDate.isValid());

        const Date xmas{ "2022", "12", "25" };
        const QString formattedText = formatDate("Today is {0}-{1}-{2}", xmas);
        QCOMPARE(formattedText, "Today is 2022-12-25");
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN(Test_CustomMarshaling)
#include "main.moc"
