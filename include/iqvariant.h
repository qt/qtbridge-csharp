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
#include <QVariant>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

struct IQVariant : public QDotNetNativeInterface<QVariant>
{
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.DotNet.IQVariant, Qt.DotNet.Adapter");

    IQVariant(const void *objectRef = nullptr)
        : QDotNetNativeInterface<QVariant>(objectRef)
    {
    }

    IQVariant(QVariant &value, bool doCleanUp = false)
        : QDotNetNativeInterface<QVariant>(AssemblyQualifiedName, &value, doCleanUp)
    {
        init();
    }

    IQVariant(const QString &value, bool doCleanUp = true)
        : QDotNetNativeInterface<QVariant>(AssemblyQualifiedName, new QVariant(value), doCleanUp)
    {
        init();
    }

    IQVariant(bool doCleanUp)
        : QDotNetNativeInterface<QVariant>(AssemblyQualifiedName, new QVariant(), doCleanUp)
    {
        init();
    }

    void init() {
        setCallback<QString>("ToStringValue", [this](void *data)
            {
                QVariant *v = reinterpret_cast<QVariant *>(data);
                if (!v)
                    return QString();
                return v->toString();
            });
        setCallback<void, QString>("SetValue", [this](void *data, const auto &newValue)
            {
                QVariant *v = reinterpret_cast<QVariant *>(data);
                if (!v)
                    return;
                v->setValue(newValue);
            });
    }

    static void staticInit(QDotNetInterface *sta)
    {
        sta->setCallback<IQVariant, QString>("QVariant_Create",
            [](void *, QString value) { return IQVariant(value, true); });
        sta->setCallback<IQVariant>("QVariant_Create",
            [](void *) { return IQVariant(true); });
    }
};
