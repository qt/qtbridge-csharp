// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetinterface.h"
#include "qdotnetadapter.h"
#include "iqtresources.h"

#include <QCoreApplication>
#include <QGuiApplication>
#include <QIcon>

struct IQtApplication : public QDotNetInterface
{
    static inline const QString AssemblyQualifiedName =
        QStringLiteral("Qt.IQtApplication, Qt.DotNet.Adapter");

    IQtApplication()
        : QDotNetInterface(AssemblyQualifiedName)
    {
        init();
    }

    void init()
    {
        setCallback<void, QString>("SetName", [](void *, const QString &name)
            { QCoreApplication::setApplicationName(name); });
        setCallback<void, QString>("SetVersion", [](void *, const QString &version)
            { QCoreApplication::setApplicationVersion(version); });
        setCallback<void, QString>("SetOrganizationName", [](void *, const QString &name)
            { QCoreApplication::setOrganizationName(name); });
        setCallback<void, QString>("SetOrganizationDomain", [](void *, const QString &domain)
            { QCoreApplication::setOrganizationDomain(domain); });
        setCallback<void, QString>("SetDisplayName", [](void *, const QString &name)
            { QGuiApplication::setApplicationDisplayName(name); });
        setCallback<void, QString>("SetWindowIcon", [](void *, const QString &url)
            { QGuiApplication::setWindowIcon(QIcon(IQtResources::toResourcePath(url))); });
    }

    static void staticInit(QDotNetInterface *sta)
    {
        static IQtApplication instance;
        sta->setCallback<IQtApplication>("QtApplication_Get", [](void *)
            {
                return IQtApplication(instance);
            });
    }
};
