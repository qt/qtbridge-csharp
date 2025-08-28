/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#pragma once

#ifndef QT_QUICK_LIB
struct IQQmlApplicationEngine {};
#else

#include "qdotnetinterface.h"
#include "qdotnetadapter.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QElapsedTimer>
#include <QQmlApplicationEngine>
#include <QThread>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

struct IQQmlApplicationEngine : public QDotNetNativeInterface<QQmlApplicationEngine>
{
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.Quick.IQQmlApplicationEngine, Qt.DotNet.Adapter");

    bool exited = false;
    int exitCode = -1;

    IQQmlApplicationEngine()
        : QDotNetNativeInterface<QQmlApplicationEngine>(AssemblyQualifiedName,
            QDotNetAdapter::instance().qmlEngine(), false)
    {
        init();
    }

    void init() {
        const auto *engine = QDotNetAdapter::instance().qmlEngine();
        if (engine == nullptr)
            return;

        QObject::connect(engine, &QQmlApplicationEngine::exit,
            [this](int code)
            {
                exitCode = code;
                exited = true;
            });
        QObject::connect(engine, &QQmlApplicationEngine::quit,
            [this]()
            {
                exitCode = 0;
                exited = true;
            });
        setCallback<void, QString, QString>("LoadFromModule", [this](void *data,
            const QString &uri, const QString &typeName)
            {
                auto *qmlEngine = reinterpret_cast<QQmlApplicationEngine *>(data);
                if (!qmlEngine)
                    return;
                QMetaObject::invokeMethod(qmlEngine, "loadFromModule", Qt::BlockingQueuedConnection,
                    Q_ARG(QAnyStringView, uri), Q_ARG(QAnyStringView, typeName));
            });
        setCallback<bool, int>("WaitForExit", [this](void *data, int timeout)
            {
                if (timeout == 0)
                    return false;
                QElapsedTimer timer;
                timer.start();
                while (!timer.hasExpired(timeout) && !IQQmlApplicationEngine::exited)
                    QThread::usleep(100);
                return IQQmlApplicationEngine::exited;
            });
    }

    static void staticInit(QDotNetInterface *sta)
    {
        static IQQmlApplicationEngine qmlEngine;
        sta->setCallback<IQQmlApplicationEngine>("QQmlApplicationEngine_Get",
            [](void *) { return IQQmlApplicationEngine(qmlEngine); });
    }
};
#endif
