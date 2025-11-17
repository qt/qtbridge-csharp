/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include <QCoreApplication>
#include <QDebug>
#include <QDir>
#include <QFile>
#include <QQmlEngine>
#include <QString>
#include <QThread>
#include <QtQuickTest>

#include <QDotNetHost>
#include <QDotNetAdapter>
#include <QDotNetStatic>

QT_DOTNET_HOST(appName);

class Setup_QtQuickTest : public QObject
{
    Q_OBJECT
private:
    QString assemblyPath;
    QDotNetHost *dotNetHost = nullptr;
    QThread *dotnetThread = nullptr;
    int dotNetResult = -1;
    bool dotNetExited = false;
public slots:
    void applicationAvailable()
    {
        dotNetHost = new QDotNetHost();
        assemblyPath = QDir(QCoreApplication::applicationDirPath()).filePath(appName);
        if (!QFile::exists(assemblyPath)) {
            qApp->exit(1);
            return;
        }

        dotnetThread = QThread::create(
            [this]()
            {
                dotNetHost->loadApp(assemblyPath);
                dotNetResult = dotNetHost->runApp();
                dotNetExited = true;
            });
        dotnetThread->start();

        int tries = 0;
        constexpr int maxTries = 10000; // ~1 sec total
        while (!dotNetExited && !dotNetHost->isReady() && tries++ < maxTries)
            QThread::usleep(100);
        if (dotNetExited) {
            qApp->exit(2);
            return;
        }
        if (!dotNetHost->isReady()) {
            qApp->exit(3);
            return;
        }
    }
    void qmlEngineAvailable(QQmlEngine *qmlEngine)
    {
        QDotNetAdapter::instance().init(
            QDir(QCoreApplication::applicationDirPath()).filePath("Qt.DotNet.Adapter.dll"),
            "Qt.DotNet.Adapter", "Qt.DotNet.Adapter", dotNetHost, qmlEngine);
        QtDotNet::call<void>("Qt.DotNet.Adapter, Qt.DotNet.Adapter", "SetMainThread");
        if (!QDotNetAdapter::instance().isValid()) {
            qApp->exit(4);
            return;
        }
        qInfo() << QString("Hello World from C++!");
    }
    void cleanupTestCase()
    {
        dotnetThread->wait(3000);
        dotNetHost->unload();
        if (!dotNetExited) {
            qApp->exit(5);
            return;
        }
        if (dotNetResult != 0) {
            qApp->exit(6);
            return;
        }
    }
};

QUICK_TEST_MAIN_WITH_SETUP(Test_QtQuickTest, Setup_QtQuickTest)

#include "main.moc"
