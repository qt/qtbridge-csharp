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

#define CALLBACK_SET_VALUE(t) \
    setCallback<void, t>("SetValue", [this](void *data, const auto &newValue) \
        { \
            QVariant *v = reinterpret_cast<QVariant *>(data); \
            if (!v) \
                return; \
            v->setValue(newValue); \
        })

#define CALLBACK_GET_VALUE(t,n,f) \
    setCallback<t>(n, [this](void *data) \
        { \
            QVariant *v = reinterpret_cast<QVariant *>(data); \
            if (!v) \
                return t(); \
            return v->f(); \
        })

#define CALLBACK_CAN_CONVERT(n,t) \
    setCallback<bool>(n, [this](void *data) \
        { \
            QVariant *v = reinterpret_cast<QVariant *>(data); \
            if (!v) \
                return false; \
            return v->canConvert<t>(); \
        })

    void init() {
        CALLBACK_CAN_CONVERT("CanConvertToBool", bool);
        CALLBACK_GET_VALUE(bool, "ToBool", toBool);
        CALLBACK_SET_VALUE(bool);

        CALLBACK_CAN_CONVERT("CanConvertToInt", int);
        CALLBACK_GET_VALUE(int, "ToInt", toInt);
        CALLBACK_SET_VALUE(int);

        CALLBACK_CAN_CONVERT("CanConvertToUInt", uint);
        CALLBACK_GET_VALUE(uint, "ToUInt", toUInt);
        CALLBACK_SET_VALUE(uint);

        CALLBACK_CAN_CONVERT("CanConvertToLongLong", qlonglong);
        CALLBACK_GET_VALUE(qlonglong, "ToLongLong", toLongLong);
        CALLBACK_SET_VALUE(qlonglong);

        CALLBACK_CAN_CONVERT("CanConvertToULongLong", qulonglong);
        CALLBACK_GET_VALUE(qulonglong, "ToULongLong", toULongLong);
        CALLBACK_SET_VALUE(qulonglong);

        CALLBACK_CAN_CONVERT("CanConvertToFloat", float);
        CALLBACK_GET_VALUE(float, "ToFloat", toFloat);
        CALLBACK_SET_VALUE(float);

        CALLBACK_CAN_CONVERT("CanConvertToDouble", double);
        CALLBACK_GET_VALUE(double, "ToDouble", toDouble);
        CALLBACK_SET_VALUE(double);

        CALLBACK_CAN_CONVERT("CanConvertToChar", QChar);
        CALLBACK_GET_VALUE(QChar, "ToChar", toChar);
        CALLBACK_SET_VALUE(QChar);

        CALLBACK_CAN_CONVERT("CanConvertToString", QString);
        CALLBACK_GET_VALUE(QString, "ToStringValue", toString);
        CALLBACK_SET_VALUE(QString);
    }

#undef CALLBACK_SET_VALUE
#undef CALLBACK_GET_VALUE
#undef CALLBACK_CAN_CONVERT

    static void staticInit(QDotNetInterface *sta)
    {
        sta->setCallback<IQVariant>("QVariant_Create",
            [](void *) { return IQVariant(true); });
    }
};
