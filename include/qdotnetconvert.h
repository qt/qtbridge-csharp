// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetref.h"

#ifdef __GNUC__
#  pragma GCC diagnostic push
#  pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QAbstractItemModel>
#include <QDateTime>
#include <QString>
#include <QUrl>
#ifdef __GNUC__
#  pragma GCC diagnostic pop
#endif

class QDotNetConvert
{
#define QDOTNET_CONVERT_TYPES(X) \
    X(bool, Boolean)             \
    X(qint8, SByte)              \
    X(quint8, Byte)              \
    X(qint16, Int16)             \
    X(quint16, UInt16)           \
    X(qint32, Int32)             \
    X(quint32, UInt32)           \
    X(qint64, Int64)             \
    X(quint64, UInt64)           \
    X(float, Single)             \
    X(double, Double)            \
    X(QChar, Char)               \
    X(QString, String)           \
    X(QDateTime, DateTime)       \
    X(QUrl, Uri)                 \
    X(QModelIndex, ModelIndex)

public:
#define DECLARE_CONVERT_FUNCTIONS(TNative, TManaged)                                      \
    static inline QDotNetRef from##TManaged(TNative arg)                                  \
    {                                                                                     \
        if (!fnFrom##TManaged.isValid()) {                                                \
            fnFrom##TManaged = adapter().resolveStaticMethod(                             \
                    "Qt.Bridge.ValueConverter, Qt.Bridge.CSharp.Api", "From" #TManaged,   \
                    { { QDotNetInbound<QDotNetRef>::Parameter,                            \
                        QDotNetOutbound<TNative>::Parameter } });                         \
        }                                                                                 \
        return fnFrom##TManaged(arg);                                                     \
    }                                                                                     \
                                                                                          \
    static inline bool is##TManaged(QDotNetRef arg)                                       \
    {                                                                                     \
        if (!fnIs##TManaged.isValid()) {                                                  \
            fnIs##TManaged = adapter().resolveStaticMethod(                               \
                    "Qt.Bridge.ValueConverter, Qt.Bridge.CSharp.Api", "Is" #TManaged,     \
                    { { QDotNetInbound<bool>::Parameter,                                  \
                        QDotNetOutbound<QDotNetRef>::Parameter } });                      \
        }                                                                                 \
        return fnIs##TManaged(arg);                                                       \
    }                                                                                     \
                                                                                          \
    static inline TNative to##TManaged(QDotNetRef arg)                                    \
    {                                                                                     \
        if (!fnTo##TManaged.isValid()) {                                                  \
            fnTo##TManaged = adapter().resolveStaticMethod(                               \
                    "Qt.Bridge.ValueConverter, Qt.Bridge.CSharp.Api", "To" #TManaged,     \
                    { { QDotNetInbound<TNative>::Parameter,                               \
                        QDotNetOutbound<QDotNetRef>::Parameter } });                      \
        }                                                                                 \
        return fnTo##TManaged(arg);                                                       \
    }

    QDOTNET_CONVERT_TYPES(DECLARE_CONVERT_FUNCTIONS)

#undef DECLARE_CONVERT_FUNCTIONS

private:
#define DECLARE_CONVERT_FIELDS(TNative, TManaged)                        \
    static inline QDotNetFunction<QDotNetRef, TNative> fnFrom##TManaged; \
    static inline QDotNetFunction<bool, QDotNetRef> fnIs##TManaged;      \
    static inline QDotNetFunction<TNative, QDotNetRef> fnTo##TManaged;

    QDOTNET_CONVERT_TYPES(DECLARE_CONVERT_FIELDS)

#undef DECLARE_CONVERT_FIELDS
#undef QDOTNET_CONVERT_TYPES

    static QDotNetAdapter &adapter() { return QDotNetAdapter::instance(); }
};
