/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#pragma once

#include <QCoreApplication>
#include <QDir>
#include <QFile>
#include <QFileInfo>
#include <QScopedPointer>
#include <QThread>
#include <QString>
#include <QDebug>

#include <QDotNetHost>
#include <QDotNetAdapter>
#include <QDotNetStatic>

QT_DOTNET_HOST(appName);

enum class BridgeExitCode
{
    Ok = 0,
    QTestFailure = 1,   // QTest will return 1 on test failure

    LocateAssemblyFailed = 101,
    WaitForReadyExitedEarly = 102,
    WaitForReadyTimeout = 103,
    InitAdapterFailed = 104,
    FinalizeNotExited = 105,
    FinalizeFailed = 106
};

class QtTestSetupBase
{
public:
    enum class ReadyResult
    {
        Ok, ExitedEarly, Timeout
    };
    enum class FinalizeResult
    {
        Ok, NotExited, Failed
    };

protected:
    QString assemblyPath;
    QString assemblyName;
    QScopedPointer<QDotNetHost> dotNetHost;
    QThread* dotnetThread = nullptr;
    int dotNetResult = -1;
    bool dotNetExited = false;

    void initHost()
    {
        if (!dotNetHost)
            dotNetHost.reset(new QDotNetHost());
    }

    bool locateAssembly()
    {
        assemblyPath = QDir(QCoreApplication::applicationDirPath()).filePath(appName);
        if (!QFile::exists(assemblyPath))
            return false;

        assemblyName = QFileInfo(assemblyPath).completeBaseName();
        return true;
    }

    bool runAppSynchronous()
    {
        if (!dotNetHost)
            initHost();

        if (!dotNetHost->loadApp(assemblyPath))
            return false;

        dotNetResult = dotNetHost->runApp();
        dotNetExited = true;
        return dotNetResult == 0;
    }

    void runAppAsynchronous()
    {
        if (!dotNetHost)
            initHost();

        dotNetExited = false;
        dotNetResult = -1;

        dotnetThread = QThread::create([this]()
        {
            dotNetHost->loadApp(assemblyPath);
            dotNetResult = dotNetHost->runApp();
            dotNetExited = true;
        });
        dotnetThread->start();
    }

    ReadyResult waitForReady(int maxTries = 10000, int sleepUsec = 100)
    {
        int tries = 0;
        while (!dotNetExited && !dotNetHost->isReady() && tries++ < maxTries)
            QThread::usleep(sleepUsec);

        if (dotNetExited)
            return ReadyResult::ExitedEarly;
        if (!dotNetHost->isReady())
            return ReadyResult::Timeout;
        return ReadyResult::Ok;
    }

    bool initAdapter(QQmlEngine* qmlEngine = nullptr, bool setMainThread = false)
    {
        QDotNetAdapter::instance()
            .init(QDir(QCoreApplication::applicationDirPath())
                .filePath("Qt.DotNet.Adapter.dll"),
                "Qt.DotNet.Adapter",
                "Qt.DotNet.Adapter",
                dotNetHost.data(),
                qmlEngine);

        if (!QDotNetAdapter::instance().isValid())
            return false;

#ifdef QT_QUICK_LIB
        if (setMainThread)
            QtDotNet::call<void>("Qt.DotNet.Adapter, Qt.DotNet.Adapter", "SetMainThread");
#else
        Q_UNUSED(setMainThread);
#endif
        return true;
    }

    FinalizeResult finalizeThreaded(int waitMs)
    {
        if (dotnetThread)
            dotnetThread->wait(waitMs);

        if (dotNetHost)
            dotNetHost->unload();

        if (!dotNetExited)
            return FinalizeResult::NotExited;
        if (dotNetResult != 0)
            return FinalizeResult::Failed;
        return FinalizeResult::Ok;
    }

    void unloadHost()
    {
        if (dotNetHost)
            dotNetHost->unload();
    }
};
