// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QAbstractListModel>
#include <QDate>
#include <QDateTime>
#include <QTest>
#include <QTime>
#include <QTimeZone>
#include <QUrl>

#include <qdotnetarray.h>
#include <qdotnetobject.h>
#include <qdotnettype.h>

#include "QtTestSetupBase.h"
#include "StringBuilder.h"

class Test_CollectionsAndValues : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    struct ModelIndexTestModel : public QAbstractListModel
    {
        int rowCount(const QModelIndex &) const override { return 0; }
        QVariant data(const QModelIndex &, int) const override { return {}; }

        QModelIndex setOwnIndex(const QModelIndex &idx)
        {
            if (idx.model() != nullptr && idx.model() != reinterpret_cast<void *>(-1))
                return QModelIndex();
            return createIndex(idx.row(), idx.column(), idx.internalId());
        }
    };

    [[nodiscard]] QString fixtureTypeName() const
    {
        return QString("%1, %2").arg("CollectionsAndValues.CollectionsFixture", assemblyName);
    }

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
    }

    void arrayOfInts()
    {
        QDotNetArray<qint32> a(11);
        a[0] = 0;
        a[1] = 1;
        for (int i = 2; i < a.length(); ++i)
            a[i] = a[i - 1] + a[i - 2];
        QCOMPARE(a[10].value(), 55);

        QVERIFY(a[10] == 55);
        QVERIFY(a[10].value() == 55);
    }

    void arrayOfStrings()
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

        QCOMPARE(a[0].value(), "Lorem");

        const auto stringType = QDotNetType::typeOf("System.String");
        const auto join = stringType.staticMethod<QString, QString, QDotNetArray<QString>>("Join");
        const auto loremIpsum = join(" ", a);

        QCOMPARE(loremIpsum, "Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
    }

    void arrayOfObjects()
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
        QCOMPARE(a[7]->toString(), "Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
    }

    void stringMarshal()
    {
        const auto echoed = QtDotNet::call<QString, QString>(fixtureTypeName(), "Echo", "hello");
        QCOMPARE(echoed, "hello");

        const auto nullString = QtDotNet::call<QString>(fixtureTypeName(), "NullString");
        QVERIFY(nullString.isNull());

        const auto emptyString = QtDotNet::call<QString>(fixtureTypeName(), "EmptyString");
        QVERIFY(emptyString.isEmpty());
        QVERIFY(!emptyString.isNull());
    }

    void fieldAccess()
    {
        // Value-type constant
        QDotNetFunction<int> fooNumberConst;
        QDotNetType::staticFieldGet(fixtureTypeName(), "FooNumber", fooNumberConst);
        QCOMPARE(fooNumberConst(), 42);

        // Ref-type constant
        QDotNetFunction<QString> fooStringConst;
        QDotNetType::staticFieldGet(fixtureTypeName(), "FooString", fooStringConst);
        QCOMPARE(fooStringConst(), "FOO");

        // Static field getter
        QDotNetFunction<int> fooStaticField;
        QDotNetType::staticFieldGet(fixtureTypeName(), "FooStaticField", fooStaticField);
        QCOMPARE(fooStaticField(), -42);

        // Static field setter
        QDotNetFunction<void, int> setFooStaticField;
        QDotNetType::staticFieldSet(fixtureTypeName(), "FooStaticField", setFooStaticField);
        setFooStaticField(123);
        QCOMPARE(fooStaticField(), 123);

        // Instance field
        const auto newFoo = QDotNetObject::constructor(fixtureTypeName());
        auto foo = newFoo();

        // Instance field getter
        QDotNetFunction<int, QDotNetRef> getFooField;
        foo.fieldGet("FooField", getFooField);
        QCOMPARE(getFooField(foo), 42);

        // Instance field setter
        QDotNetFunction<void, QDotNetRef, int> setFooField;
        foo.fieldSet("FooField", setFooField);
        setFooField(foo, 123);
        QCOMPARE(getFooField(foo), 123);
    }

    void modelIndexMarshal()
    {
        ModelIndexTestModel model;
        const auto idx = model.setOwnIndex(QtDotNet::call<QModelIndex>(fixtureTypeName(), "FindIndex"));

        QVERIFY(idx.isValid());
        QCOMPARE(idx.row(), 42);
        QCOMPARE(idx.column(), 24);
        QCOMPARE(idx.internalId(), quintptr(0x12345678));
        QCOMPARE(idx.model(), &model);

        const auto dataAt = QtDotNet::call<QString, QModelIndex>(fixtureTypeName(), "DataAt", idx);
        QCOMPARE(dataAt, "42, 24, 0x12345678");
    }

    void dateTimeMarshal()
    {
        const auto dt = QtDotNet::call<QDateTime>(fixtureTypeName(), "GetDateTime");
        QCOMPARE(dt, QDateTime(QDate(1912, 6, 23), QTime(11, 22, 33, 444), QTimeZone::utc()));

        const auto printedDateTime
            = QtDotNet::call<QString, QDateTime>(fixtureTypeName(), "PrintDateTime", dt);
        QCOMPARE(printedDateTime, "1912-06-23 11:22:33.444");
    }

    void uriMarshal()
    {
        const auto url = QtDotNet::call<QUrl>(fixtureTypeName(), "GetUri");
        QCOMPARE(url.toDisplayString(), "https://www.qt.io/developers#wiki");

        const auto printedUri = QtDotNet::call<QString, QUrl>(fixtureTypeName(), "PrintUri", url);
        QCOMPARE(printedUri, "https://www.qt.io/developers#wiki");
    }

    void qcharWidthDiffersFromWcharOnThisPlatform()
    {
        QCOMPARE(sizeof(QChar), size_t(2));
#ifdef Q_OS_WIN
        QCOMPARE(sizeof(wchar_t), sizeof(QChar));
#else
        QCOMPARE(sizeof(wchar_t), size_t(4));
        QVERIFY(sizeof(wchar_t) != sizeof(QChar));
#endif
    }

    void utf16TerminatedCopyPreservesTail()
    {
#ifdef Q_OS_WIN
        QSKIP("This regression test is only meaningful where wchar_t and QChar differ.");
#else
        const QChar source[] = {
            QChar(u'A'),
            QChar(u'B'),
            QChar(u'C'),
            QChar(u'\0'),
            QChar(u'Y'),
            QChar(u'Z')
        };
        constexpr qsizetype logicalLength = 3;
        const qsizetype expectedUtf16Bytes = qDotNetUtf16TerminatedByteCount(logicalLength);
        const qsizetype allocatedBytes = expectedUtf16Bytes + 8;

        QByteArray destination(static_cast<qsizetype>(allocatedBytes), char(0x5A));

        qDotNetCopyUtf16TerminatedBuffer(destination.data(), source, logicalLength);

        QCOMPARE(destination.first(expectedUtf16Bytes),
            QByteArray(reinterpret_cast<const char *>(source), expectedUtf16Bytes));
        QCOMPARE(destination.sliced(expectedUtf16Bytes),
            QByteArray(static_cast<qsizetype>(allocatedBytes - expectedUtf16Bytes), char(0x5A)));
#endif
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN_WITH_DOTNET_SETUP(Test_CollectionsAndValues)
#include "main.moc"
