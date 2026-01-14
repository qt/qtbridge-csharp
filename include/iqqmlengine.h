// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#ifndef QT_QUICK_LIB
struct IQQmlEngine {};
#else

#include "qdotnetinterface.h"
#include "qdotnetadapter.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QElapsedTimer>
#include <QQmlEngine>
#include <QThread>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

struct IQQmlEngine : public QDotNetNativeInterface<QQmlEngine>
{
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.Quick.IQQmlEngine, Qt.DotNet.Adapter");

    bool exited = false;
    int exitCode = -1;

    IQQmlEngine()
        : QDotNetNativeInterface<QQmlEngine>(AssemblyQualifiedName,
            QDotNetAdapter::instance().qmlEngine(), false)
    {
        init();
    }

    void init() {
        const auto *engine = QDotNetAdapter::instance().qmlEngine();
        if (engine == nullptr)
            return;

        QObject::connect(engine, &QQmlEngine::exit,
            [this](int code)
            {
                exitCode = code;
                exited = true;
            });
        QObject::connect(engine, &QQmlEngine::quit,
            [this]()
            {
                exitCode = 0;
                exited = true;
            });
        setCallback<void, QString, QString>("LoadFromModule", [this](void *data,
            const QString &uri, const QString &typeName)
            {
                auto *qmlEngine = reinterpret_cast<QQmlEngine *>(data);
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
                while (!timer.hasExpired(timeout) && !IQQmlEngine::exited)
                    QThread::usleep(100);
                return IQQmlEngine::exited;
            });
        setCallback<void>("ProcessEvents", [this](void *data)
            {
                QCoreApplication::processEvents();
            });
    }

    static void staticInit(QDotNetInterface *sta)
    {
        static IQQmlEngine qmlEngine;
        sta->setCallback<IQQmlEngine>("QQmlEngine_Get",
            [](void *) { return IQQmlEngine(qmlEngine); });
    }
};
#endif
