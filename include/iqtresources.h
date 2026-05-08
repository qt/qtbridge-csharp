// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetinterface.h"
#include "qdotnetadapter.h"

#include <QFile>

struct IQtResources : public QDotNetInterface
{
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.IQtResources, Qt.DotNet.Adapter");

    IQtResources()
        : QDotNetInterface(AssemblyQualifiedName)
    {
        init();
    }

    void init()
    {
        setCallback<bool, QString>("Exists", [](void *, const QString &url)
            {
                return QFile::exists(toResourcePath(url));
            });
        setCallback<qint32, QString>("Size", [](void *, const QString &url) -> qint32
            {
                QFile file(toResourcePath(url));
                if (!file.open(QIODevice::ReadOnly))
                    return -1;
                return static_cast<qint32>(file.size());
            });
        setCallback<qint32, QString, void *, qint32>("Read",
            [](void *, const QString &url, void *dest, qint32 destLen) -> qint32
            {
                QFile file(toResourcePath(url));
                if (!file.open(QIODevice::ReadOnly))
                    return -1;
                return static_cast<qint32>(file.read(static_cast<char *>(dest), destLen));
            });
    }

    static QString toResourcePath(const QString &url)
    {
        if (url.startsWith(QStringLiteral("qrc:/")))
            return QStringLiteral(":") + url.mid(4);
        return url;
    }

    static void staticInit(QDotNetInterface *sta)
    {
        static IQtResources qtResources;
        sta->setCallback<IQtResources>("QtResources_Get", [](void *)
            {
                return IQtResources(qtResources);
            });
    }
};
