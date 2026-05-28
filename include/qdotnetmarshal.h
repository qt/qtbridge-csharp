// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetparameter.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QChar>
#include <QDateTime>
#include <QList>
#include <QModelIndex>
#include <QString>
#include <QTimeZone>
#include <QUrl>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <type_traits>

#ifdef Q_OS_WINDOWS
#   include <windows.h>
#else
#   include <stdlib.h>
#endif

struct QDotNetMarshal
{
    static void *allocHGlobal(size_t size)
    {
#ifdef Q_OS_WINDOWS
        return LocalAlloc(LMEM_FIXED | LMEM_ZEROINIT, size);
#else
        void *ptr = malloc(size);
        memset(ptr, 0, size);
        return ptr;
#endif
    }

    static void freeHGlobal(void *ptr)
    {
#ifdef Q_OS_WINDOWS
        LocalFree(ptr);
#else
        free(ptr);
#endif
    }
};

class QDotNetException;
class QDotNetRef;
class QDotNetType;

template<typename T, typename Enable = void>
struct QDotNetIsRef : std::false_type
{};

template<typename T>
struct QDotNetIsRef<T, std::void_t<decltype(sizeof(T))>>
    : std::bool_constant<std::is_base_of_v<QDotNetRef, T>>
{};

template<>
struct QDotNetIsRef<QDotNetRef> : std::true_type
{};

template<typename T>
inline constexpr bool QDotNetIsRef_v = QDotNetIsRef<T>::value;

namespace QtDotNet::TypeNames::System
{
    static constexpr char Void[]{ "System.Void" };
    static constexpr char Boolean[]{ "System.Boolean" };
    static constexpr char Single[]{ "System.Single" };
    static constexpr char Double[]{ "System.Double" };
    static constexpr char Byte[]{ "System.Byte" };
    static constexpr char SByte[]{ "System.SByte" };
    static constexpr char Int16[]{ "System.Int16" };
    static constexpr char UInt16[]{ "System.UInt16" };
    static constexpr char Int32[]{ "System.Int32" };
    static constexpr char UInt32[]{ "System.UInt32" };
    static constexpr char Int64[]{ "System.Int64" };
    static constexpr char UInt64[]{ "System.UInt64" };
    static constexpr char IntPtr[]{ "System.IntPtr" };
    static constexpr char UIntPtr[]{ "System.UIntPtr" };
    static constexpr char Char[]{ "System.Char" };
    static constexpr char String[]{ "System.String" };
    static constexpr char Object[]{ "System.Object" };
    static constexpr char Type[]{ "System.Type" };
}

template<typename T, typename Enable = void>
struct QDotNetTypeOf;

template<typename T, typename Enable = void>
struct QDotNetOutbound;

template<typename T, typename Enable = void>
struct QDotNetInbound;

template<typename T, typename Enable = void>
struct QDotNetNull
{
    static T value() { return T(); }
    static bool isNull(const T &obj) { return obj == value(); }
};

template<typename T>
struct QDotNetOutbound<QDotNetNull<T>>
{
    using SourceType = typename QDotNetOutbound<T>::SourceType;
    using OutboundType = typename QDotNetOutbound<T>::OutboundType;
    static inline const QDotNetParameter Parameter = QDotNetOutbound<T>::Parameter;
    static OutboundType convert(SourceType sourceValue)
    {
        return QDotNetNull<OutboundType>::value();
    }
};

template<typename T>
struct QDotNetInbound<QDotNetNull<T>>
{
    using InboundType = typename QDotNetInbound<T>::InboundType;
    using TargetType = typename QDotNetInbound<T>::TargetType;
    static inline const QDotNetParameter Parameter = QDotNetInbound<T>::Parameter;
    static TargetType convert(InboundType inboundValue)
    {
        return QDotNetNull<InboundType>::value();
    }
};

#define Q_DOTNET_TYPEOF(T,typeName,marshalAs)\
template<>\
struct QDotNetTypeOf<T>\
{\
    static inline const QString TypeName = QString(QtDotNet::TypeNames::typeName);\
    static inline UnmanagedType MarshalAs = marshalAs;\
}

Q_DOTNET_TYPEOF(void, System::Void, UnmanagedType::Void);
Q_DOTNET_TYPEOF(bool, System::Boolean, UnmanagedType::Bool);
Q_DOTNET_TYPEOF(qint8, System::SByte, UnmanagedType::I1);
Q_DOTNET_TYPEOF(quint8, System::Byte, UnmanagedType::U1);
Q_DOTNET_TYPEOF(qint16, System::Int16, UnmanagedType::I2);
Q_DOTNET_TYPEOF(quint16, System::UInt16, UnmanagedType::U2);
Q_DOTNET_TYPEOF(qint32, System::Int32, UnmanagedType::I4);
Q_DOTNET_TYPEOF(quint32, System::UInt32, UnmanagedType::U4);
Q_DOTNET_TYPEOF(qint64, System::Int64, UnmanagedType::I8);
Q_DOTNET_TYPEOF(quint64, System::UInt64, UnmanagedType::U8);
Q_DOTNET_TYPEOF(void *, System::IntPtr, UnmanagedType::SysInt);
Q_DOTNET_TYPEOF(nullptr_t, System::IntPtr, UnmanagedType::SysInt);
Q_DOTNET_TYPEOF(float, System::Single, UnmanagedType::R4);
Q_DOTNET_TYPEOF(double, System::Double, UnmanagedType::R8);

template<typename T>
struct QDotNetOutbound<T, std::enable_if_t<std::is_fundamental_v<T>>>
{
    using SourceType = T;
    using OutboundType = T;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<T>::TypeName, QDotNetTypeOf<T>::MarshalAs);
    static OutboundType convert(SourceType sourceValue)
    {
        return sourceValue;
    }
};

template<typename T>
struct QDotNetInbound<T, std::enable_if_t<std::is_fundamental_v<T>>>
{
    using InboundType = T;
    using TargetType = T;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<T>::TypeName, QDotNetTypeOf<T>::MarshalAs);
    static TargetType convert(InboundType inboundValue)
    {
        return inboundValue;
    }
};

template<typename T>
struct QDotNetNull<T, std::enable_if_t<std::is_fundamental_v<T>>>
{
    static T value() { return T(0); }
    static bool isNull(const T &obj) { return obj == value(); }
};

template<>
struct QDotNetTypeOf<QChar>
{
    static inline const QString TypeName = QStringLiteral("System.Char");
    static inline UnmanagedType MarshalAs = UnmanagedType::U2; // 16-bit
};

template<>
struct QDotNetOutbound<QChar>
{
    using SourceType = QChar;
    using OutboundType = quint16; // matches U2
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<QChar>::TypeName, QDotNetTypeOf<QChar>::MarshalAs);
    static OutboundType convert(SourceType sourceValue) { return sourceValue.unicode(); }
};

template<>
struct QDotNetInbound<QChar>
{
    using InboundType = quint16;
    using TargetType = QChar;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<QChar>::TypeName, QDotNetTypeOf<QChar>::MarshalAs);
    static TargetType convert(InboundType inboundValue) { return { inboundValue }; }
};

template<typename T>
struct QDotNetOutbound<T, std::enable_if_t<std::is_pointer_v<T>>>
{
    using SourceType = T;
    using OutboundType = void*;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<void *>::TypeName, QDotNetTypeOf<void *>::MarshalAs);
    static OutboundType convert(SourceType srvValue)
    {
        return reinterpret_cast<void *>(srvValue);
    }
};

template<typename T>
struct QDotNetInbound<T, std::enable_if_t<std::is_pointer_v<T>>>
{
    using InboundType = T;
    using TargetType = T;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<T>::TypeName, QDotNetTypeOf<T>::MarshalAs);
    static TargetType convert(InboundType inboundValue)
    {
        return reinterpret_cast<T>(inboundValue);
    }
};

template<typename T>
struct QDotNetNull<T, std::enable_if_t<std::is_pointer_v<T>>>
{
    static T value() { return nullptr; }
    static bool isNull(const T &ptr) { return ptr == value(); }
};

template<>
struct QDotNetOutbound<void>
{
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<void>::TypeName, QDotNetTypeOf<void>::MarshalAs);
};

template<>
struct QDotNetInbound<void>
{
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<void>::TypeName, QDotNetTypeOf<void>::MarshalAs);
};

template<>
struct QDotNetTypeOf<QString>
{
    static inline const QString TypeName = QStringLiteral("System.String");
    static inline UnmanagedType MarshalAs = UnmanagedType::LPWStr;
};

template<>
struct QDotNetOutbound<QString>
{
    using SourceType = const QString&;
    using OutboundType = const QChar*;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<QString>::TypeName, QDotNetTypeOf<QString>::MarshalAs);
    static OutboundType convert(SourceType sourceValue)
    {
        return sourceValue.data();
    }
};

template<>
struct QDotNetInbound<QString>
{
    using InboundType = const QChar*;
    using TargetType = QString;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<QString>::TypeName, QDotNetTypeOf<QString>::MarshalAs);
    static TargetType convert(InboundType inboundValue)
    {
        return QString(inboundValue);
    }
};

template<>
struct QDotNetNull<QString>
{
    static QString value() { return {}; }
    static bool isNull(const QString &str) { return str.isNull() || str.isEmpty(); }
};

template<typename T>
struct QDotNetOutbound<QList<T>, void>
{
    using SourceType = const QList<T>&;
    using OutboundType = const T*;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(UnmanagedType::LPArray);
    static OutboundType convert(SourceType srvValue)
    {
        return srvValue.constData();
    }
};

template<>
struct QDotNetTypeOf<QDotNetRef>
{
    static inline const QString TypeName = QStringLiteral("System.Object");
    static inline UnmanagedType MarshalAs = UnmanagedType::ObjectRef;
};

template<>
struct QDotNetTypeOf<QDotNetType>
{
    static inline const QString TypeName = QStringLiteral("System.Type");
    static inline UnmanagedType MarshalAs = UnmanagedType::ObjectRef;
};

template<>
struct QDotNetTypeOf<QDotNetException>
{
    static inline const QString TypeName = QStringLiteral("System.Exception");
    static inline UnmanagedType MarshalAs = UnmanagedType::ObjectRef;
};

template<typename T>
struct QDotNetOutbound<T, std::enable_if_t<QDotNetIsRef_v<T>>>
{
    using SourceType = const T&;
    using OutboundType = const void*;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<T>::TypeName, QDotNetTypeOf<T>::MarshalAs);
    static OutboundType convert(SourceType dotNetObj)
    {
        return dotNetObj.gcHandle();
    }
};

template<typename T>
struct QDotNetInbound<T, std::enable_if_t<QDotNetIsRef_v<T>>>
{
    using InboundType = const void*;
    using TargetType = T;
    static inline const QDotNetParameter Parameter =
        QDotNetParameter(QDotNetTypeOf<T>::TypeName, QDotNetTypeOf<T>::MarshalAs);
    static TargetType convert(InboundType gcHandle)
    {
        return T(gcHandle);
    }
};

template<typename T>
struct QDotNetNull<T, std::enable_if_t<QDotNetIsRef_v<T>>>
{
    static T value() { return T(nullptr); }
    static bool isNull(const T &obj) { return !obj.isValid(); }
};

namespace QtDotNet
{
    using Null = QDotNetNull<QDotNetRef>;

    template<typename T>
    bool isNull(const T &obj) { return QDotNetNull<T>::isNull(obj); }

    template<typename T>
    T null() { return QDotNetNull<T>::value(); }
}

template<>
struct QDotNetTypeOf<QModelIndex>
{
    static inline const QString TypeName =
        QStringLiteral("Qt.DotNet.ModelIndexMarshaler, Qt.DotNet.Adapter");
    static inline UnmanagedType MarshalAs = UnmanagedType::CustomMarshaler;
};

template<>
struct QDotNetOutbound<QModelIndex>
{
    using SourceType = const QModelIndex &;
    using OutboundType = const void *;
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<QModelIndex>::TypeName, QDotNetTypeOf<QModelIndex>::MarshalAs);
    static OutboundType convert(SourceType sourceValue)
    {
        return reinterpret_cast<OutboundType>(&sourceValue);
    }
};

template<>
struct QDotNetInbound<QModelIndex>
{
    using InboundType = void *;
    using TargetType = QModelIndex;
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<QModelIndex>::TypeName, QDotNetTypeOf<QModelIndex>::MarshalAs);
    static QModelIndex convert(void *inboundValue)
    {
        if (!inboundValue)
            return QModelIndex();
        QModelIndex idx = *reinterpret_cast<QModelIndex *>(inboundValue);
        QDotNetMarshal::freeHGlobal(inboundValue);
        return idx;
    }
};

template<>
struct QDotNetTypeOf<QDateTime>
{
    static inline const QString TypeName = QStringLiteral("System.DateTime");
    static inline UnmanagedType MarshalAs = UnmanagedType::Struct;
    static constexpr double Epoch = 25569.0;
    static constexpr double Scale = 86400000.0;
};

template<>
struct QDotNetOutbound<QDateTime>
{
    using SourceType = const QDateTime &;
    using OutboundType = double;
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<QDateTime>::TypeName, QDotNetTypeOf<QDateTime>::MarshalAs);
    static OutboundType convert(SourceType sourceValue)
    {
        return (sourceValue.toUTC().toMSecsSinceEpoch() / QDotNetTypeOf<QDateTime>::Scale)
            + QDotNetTypeOf<QDateTime>::Epoch;
    }
};

template<>
struct QDotNetInbound<QDateTime>
{
    using InboundType = double;
    using TargetType = QDateTime;
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<QDateTime>::TypeName, QDotNetTypeOf<QDateTime>::MarshalAs);
    static TargetType convert(InboundType inboundValue)
    {
        return QDateTime::fromMSecsSinceEpoch(qint64(
            (inboundValue - QDotNetTypeOf<QDateTime>::Epoch) * QDotNetTypeOf<QDateTime>::Scale),
            QTimeZone::utc());
    }

};

template<>
struct QDotNetTypeOf<QUrl>
{
    static inline const QString TypeName =
        QStringLiteral("Qt.DotNet.UriMarshaler, Qt.DotNet.Adapter");
    static inline UnmanagedType MarshalAs = UnmanagedType::CustomMarshaler;
};

template<>
struct QDotNetOutbound<QUrl>
{
    using SourceType = const QUrl &;
    using OutboundType = void *;
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<QUrl>::TypeName, QDotNetTypeOf<QUrl>::MarshalAs);
    static OutboundType convert(SourceType sourceValue)
    {
        QString url = sourceValue.toDisplayString();
        void *ptr = QDotNetMarshal::allocHGlobal((url.length() + 1) * sizeof(wchar_t));
        memcpy(ptr, url.constData(), url.length() * sizeof(wchar_t));
        return ptr;
    }
};

template<>
struct QDotNetInbound<QUrl>
{
    using InboundType = QChar *;
    using TargetType = QUrl;
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<QUrl>::TypeName, QDotNetTypeOf<QUrl>::MarshalAs);
    static TargetType convert(InboundType inboundValue)
    {
        if (!inboundValue)
            return QUrl();
        auto url = QUrl(QString(inboundValue));
        QDotNetMarshal::freeHGlobal(inboundValue);
        return url;
    }
};
