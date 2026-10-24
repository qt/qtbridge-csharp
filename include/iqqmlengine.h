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
#include <QDeadlineTimer>
#include <QMutex>
#include <QQmlEngine>
#include <QWaitCondition>
#include <QtVersion>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

struct IQQmlEngine : public QDotNetNativeInterface<QQmlEngine>
{
    static inline const QString AssemblyQualifiedName =
        QStringLiteral("Qt.Quick.IQQmlEngine, Qt.DotNet.Adapter");

    // Set on the GUI thread when QML quits; WaitForExit() blocks on them in the .NET thread.
    // Static, as there is one QML engine and the interface is handed out by value. exited stays
    // per instance: the callbacks are bound to the static instance from staticInit().
    static inline QMutex exitMutex;
    static inline QWaitCondition exitCondition;
    bool exited = false;
    int exitCode = -1;

    void setExited(int code)
    {
        QMutexLocker lock(&exitMutex);
        exitCode = code;
        exited = true;
        exitCondition.wakeAll();
    }

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

        QObject::connect(engine, &QQmlEngine::exit, [this](int code) { setExited(code); });
        QObject::connect(engine, &QQmlEngine::quit, [this]() { setExited(0); });
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
                const QDeadlineTimer deadline = timeout < 0
                    ? QDeadlineTimer(QDeadlineTimer::Forever) : QDeadlineTimer(timeout);
                QMutexLocker lock(&exitMutex);
                while (!exited) {
                    if (!exitCondition.wait(&exitMutex, deadline))
                        break;
                }
                return exited;
            });
        setCallback<void>("ProcessEvents", [this](void *data)
            {
                QCoreApplication::processEvents();
            });
        setCallback<QString>("Version", [this](void *data)
            {
                return qVersion();
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
