// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#ifndef QT_QUICK_LIB
struct IQQmlEngine {};
#else

#include "qdotnetinterface.h"
#include "qdotnetadapter.h"
#include "qdotnetarray.h"
#include "qdotnetconvert.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QDeadlineTimer>
#include <QJSEngine>
#include <QMutex>
#include <QQmlComponent>
#include <QQmlEngine>
#include <QVariant>
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
        setCallback<void, QString, QString, QDotNetArray<QString>, QDotNetArray<QDotNetRef>>(
            "LoadFromModule", [this](void *data, const QString &uri, const QString &typeName,
                       const QDotNetArray<QString> &initPropsNames,
                       const QDotNetArray<QDotNetRef> &initPropsValues)
            {
                auto *qmlEngine = reinterpret_cast<QQmlEngine *>(data);
                if (!qmlEngine)
                    return;

                auto initPropsCount = initPropsValues.length();
                if (initPropsNames.length() != initPropsCount) {
                    qWarning() << "Property name/value mismatch";
                    return;
                }

                QMetaObject::invokeMethod(qmlEngine, [=]
                {
                    QVariantMap initProps{ };
                    for (int i = 0; i < initPropsCount; ++i) {
                        auto name = initPropsNames.get(i);
                        auto dotNetObj = initPropsValues.get(i);
                        if (!dotNetObj.isValid()) {
                            initProps[name] = QVariant::fromValue<QObject *>(nullptr);
                            continue;
                        }
                        auto value = QDotNetConvert::toVariant(dotNetObj, nullptr);
                        if (auto *qObj = qvariant_cast<QObject *>(value))
                            QJSEngine::setObjectOwnership(qObj, QJSEngine::JavaScriptOwnership);
                        initProps[name] = value;
                    }
                    componentloadFromModule(qmlEngine, uri, typeName, initProps);
                },
                Qt::QueuedConnection);
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

private:
    void componentloadFromModule(QQmlEngine *qmlEngine, const QString &uri, const QString &typeName,
                                 QVariantMap initProps)
    {
        auto *component = new QQmlComponent(qmlEngine);
        QObject::connect(component, &QQmlComponent::statusChanged, [=](QQmlComponent::Status status)
        {
            componentStatusChanged(component, status, initProps);
        });
        component->loadFromModule(uri, typeName, QQmlComponent::Asynchronous);
    }

    void componentStatusChanged(QQmlComponent *component, QQmlComponent::Status status,
                                QVariantMap initProps)
    {
        switch (status) {
        case QQmlComponent::Ready:
            if (auto *object = component->createWithInitialProperties(initProps))
                object->setParent(component->engine());
            else
                qWarning().noquote() << component->errorString();
            component->deleteLater();
            break;
        case QQmlComponent::Error:
            qWarning().noquote() << component->errorString();
            component->deleteLater();
            break;
        }
    }
};
#endif
