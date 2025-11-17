/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include <QCoreApplication>
#include <QDebug>
#include <QDir>
#include <QFile>
#include <QFileInfo>
#include <QString>
#include <QTest>

#include <QDotNetHost>
#include <QDotNetAdapter>
#include <QDotNetStatic>

QT_DOTNET_HOST(appName);

class Test_QtTest : public QObject
{
    Q_OBJECT
private:
    QString assemblyPath;
    QString assemblyName;
    QDotNetHost *dotNetHost = nullptr;
private slots:
    void initTestCase()
    {
        dotNetHost = new QDotNetHost();
    }
    void assemblyExists()
    {
        assemblyPath = QDir(QCoreApplication::applicationDirPath()).filePath(appName);
        QVERIFY(QFile::exists(assemblyPath));
        assemblyName = QFileInfo(assemblyPath).completeBaseName();
    }
    void dotnetMain()
    {
        QVERIFY(dotNetHost->loadApp(assemblyPath));
        QCOMPARE(dotNetHost->runApp(), 0);
    }
    void initAdapter()
    {
        QDotNetAdapter::instance().init(
            QDir(QCoreApplication::applicationDirPath()).filePath("Qt.DotNet.Adapter.dll"),
            "Qt.DotNet.Adapter", "Qt.DotNet.Adapter", dotNetHost);
        QVERIFY(QDotNetAdapter::instance().isValid());
        qInfo() << QString("Hello World from C++!");
    }
    void callStatic()
    {
        auto fortyTwo = QString("%1, %2").arg("QtTest.FortyTwo", assemblyName);
        QCOMPARE(QtDotNet::call<int>(fortyTwo, "get_Value"), 42);
    }
    void cleanupTestCase()
    {
        dotNetHost->unload();
    }
};

QTEST_MAIN(Test_QtTest)
#include "main.moc"
