// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QSignalSpy>
#include <QTest>

#include "QtTestSetupBase.h"
#include "TransformedTextSource.h"

class UppercaseTransformation final : public NativeTextTransformation
{
public:
    QString transform(const QString &text) const override
    {
        return text.toUpper();
    }

    QUrl getUri(int) const override
    {
        return uri;
    }

    void setUri(const QUrl &value) const override
    {
        uri = value;
    }

    int getNumber() const override
    {
        return 42;
    }

private:
    mutable QUrl uri = QUrl(QStringLiteral("https://qt.io/"));
};

class Test_BindingsAndInterfaces : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
        TransformedTextSource::setAssemblyName(assemblyName);
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
    }

    void propertyBinding()
    {
        TransformedTextSource source;
        const QSignalSpy spy(&source, &TransformedTextSource::textChanged);

        QVERIFY(source.isValid());
        for (int i = 0; i < 1000; ++i)
            source.setText(QString("hello x %1").arg(i + 1));

        QCOMPARE(source.text(), "hello x 1000");
        QCOMPARE(spy.count(), 1000);
    }

    void implementInterface()
    {
        const UppercaseTransformation transformation;
        TransformedTextSource source(transformation);

        QVERIFY(source.isValid());
        source.setText("hello there");
        QCOMPARE(source.text(), "HELLO THERE (https://qt.io/developers)");
    }

    void cleanupTestCase()
    {
        unloadHost();
    }
};

QTEST_MAIN_WITH_DOTNET_SETUP(Test_BindingsAndInterfaces)
#include "main.moc"
