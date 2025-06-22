/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#pragma once

#include "qdotnetinterface.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QModelIndex>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

struct IQModelIndex : public QDotNetNativeInterface<QModelIndex>
{
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.DotNet.IQModelIndex, Qt.DotNet.Adapter");

    IQModelIndex(const void *objectRef = nullptr)
        : QDotNetNativeInterface<QModelIndex>(objectRef)
    {
    }

    IQModelIndex(const QModelIndex &idx)
        : QDotNetNativeInterface<QModelIndex>(AssemblyQualifiedName, new QModelIndex(idx), true)
    {
        init();
    }

    IQModelIndex(bool doCleanUp)
        : QDotNetNativeInterface<QModelIndex>(AssemblyQualifiedName, new QModelIndex(), doCleanUp)
    {
        init();
    }

    void init() {
        setCallback<bool>("IsValid", [this](void *data)
            {
                return reinterpret_cast<QModelIndex *>(data)->isValid();
            });
        setCallback<int>("Column", [this](void *data)
            {
                return reinterpret_cast<QModelIndex *>(data)->column();
            });
        setCallback<int>("Row", [this](void *data)
            {
                return reinterpret_cast<QModelIndex *>(data)->row();
            });
        setCallback<void *>("InternalPointer", [this](void *data)
            {
                return reinterpret_cast<QModelIndex *>(data)->internalPointer();
            });
    }

    static void staticInit(QDotNetInterface *sta)
    {
        sta->setCallback<IQModelIndex>("QModelIndex_Create",
            [](void *) { return IQModelIndex(true); });
    }
};
