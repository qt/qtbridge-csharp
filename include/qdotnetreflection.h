// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetarray.h"
#include "qdotnetref.h"

#ifdef __GNUC__
#  pragma GCC diagnostic push
#  pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QList>
#include <QString>
#ifdef __GNUC__
#  pragma GCC diagnostic pop
#endif

class QDotNetAssembly : public QDotNetRef
{
public:
    static inline const QString &AssemblyQualifiedName =
            QStringLiteral("System.Reflection.Assembly");

    QDotNetAssembly(const void *objRef = nullptr) : QDotNetRef(objRef) { }

    QDotNetAssembly(const QDotNetRef &cpySrc) : QDotNetRef(adapter().addObjectRef(&cpySrc)) { }

    QDotNetAssembly &operator=(const QDotNetRef &cpySrc)
    {
        QDotNetRef::operator=(cpySrc);
        return *this;
    }

    QDotNetAssembly(QDotNetRef &&movSrc) noexcept : QDotNetRef(std::move(movSrc)) { }

    QDotNetAssembly &operator=(QDotNetRef &&movSrc) noexcept
    {
        QDotNetRef::operator=(std::move(movSrc));
        return *this;
    }

    QDotNetRef getType(const QString &typeName) const
    {
        if (!isValid())
            return nullptr;

        if (!fnGetType.isValid()) {
            const QList<QDotNetParameter> parameters{ QDotNetInbound<QDotNetRef>::Parameter,
                                                      QDotNetOutbound<QString>::Parameter };
            fnGetType = adapter().resolveInstanceMethod(*this, "GetType", parameters);
        }
        return fnGetType(typeName);
    }

private:
    mutable QDotNetFunction<QDotNetRef, QString> fnGetType;
};

class QDotNetModule : public QDotNetRef
{
public:
    static inline const QString &AssemblyQualifiedName = QStringLiteral("System.Reflection.Module");

    QDotNetModule(const void *objRef = nullptr) : QDotNetRef(objRef) { }

    QDotNetModule(const QDotNetRef &cpySrc) : QDotNetRef(adapter().addObjectRef(&cpySrc)) { }

    QDotNetModule &operator=(const QDotNetRef &cpySrc)
    {
        QDotNetRef::operator=(cpySrc);
        return *this;
    }

    QDotNetModule(QDotNetRef &&movSrc) noexcept : QDotNetRef(std::move(movSrc)) { }

    QDotNetModule &operator=(QDotNetRef &&movSrc) noexcept
    {
        QDotNetRef::operator=(std::move(movSrc));
        return *this;
    }

    QDotNetRef resolveMethod(int metadataToken) const
    {
        if (!isValid())
            return nullptr;

        if (!fnResolveMethod.isValid()) {
            const QList<QDotNetParameter> parameters{ QDotNetInbound<QDotNetRef>::Parameter,
                                                      QDotNetOutbound<int>::Parameter };
            fnResolveMethod = adapter().resolveInstanceMethod(*this, "ResolveMethod", parameters);
        }
        return fnResolveMethod(metadataToken);
    }

private:
    mutable QDotNetFunction<QDotNetRef, int> fnResolveMethod;
};

class QDotNetMethodInfo : public QDotNetRef
{
public:
    static inline const QString &AssemblyQualifiedName =
            QStringLiteral("System.Reflection.MethodInfo");

    QDotNetMethodInfo(const void *objRef = nullptr) : QDotNetRef(objRef) { }

    QDotNetMethodInfo(const QDotNetRef &cpySrc) : QDotNetRef(adapter().addObjectRef(&cpySrc)) { }

    QDotNetMethodInfo &operator=(const QDotNetRef &cpySrc)
    {
        QDotNetRef::operator=(cpySrc);
        return *this;
    }

    QDotNetMethodInfo(QDotNetRef &&movSrc) noexcept : QDotNetRef(std::move(movSrc)) { }

    QDotNetMethodInfo &operator=(QDotNetRef &&movSrc) noexcept
    {
        QDotNetRef::operator=(std::move(movSrc));
        return *this;
    }

    QDotNetRef invoke(QDotNetRef obj, QDotNetArray<QDotNetRef> parameters) const
    {
        if (!isValid())
            return nullptr;

        if (!fnInvoke.isValid()) {
            fnInvoke = adapter().resolveInstanceMethod(
                    *this, "Invoke",
                    { QDotNetInbound<QDotNetRef>::Parameter, QDotNetOutbound<QDotNetRef>::Parameter,
                      QDotNetOutbound<QDotNetArray<QDotNetRef>>::Parameter });
        }
        return fnInvoke(obj, parameters);
    }

private:
    mutable QDotNetFunction<QDotNetRef, QDotNetRef, QDotNetArray<QDotNetRef>> fnInvoke;
};

class QDotNetPropertyInfo : public QDotNetRef
{
public:
    static inline const QString &AssemblyQualifiedName =
            QStringLiteral("System.Reflection.PropertyInfo");

    QDotNetPropertyInfo(const void *objRef = nullptr) : QDotNetRef(objRef) { }

    QDotNetPropertyInfo(const QDotNetRef &cpySrc) : QDotNetRef(adapter().addObjectRef(&cpySrc)) { }

    QDotNetPropertyInfo &operator=(const QDotNetRef &cpySrc)
    {
        QDotNetRef::operator=(cpySrc);
        return *this;
    }

    QDotNetPropertyInfo(QDotNetRef &&movSrc) noexcept : QDotNetRef(std::move(movSrc)) { }

    QDotNetPropertyInfo &operator=(QDotNetRef &&movSrc) noexcept
    {
        QDotNetRef::operator=(std::move(movSrc));
        return *this;
    }

    QDotNetRef getValue(QDotNetRef obj, QDotNetArray<QDotNetRef> parameters = nullptr) const
    {
        if (!isValid())
            return nullptr;

        if (!fnGet.isValid()) {
            fnGet = adapter().resolveInstanceMethod(
                    *this, "GetValue",
                    { QDotNetInbound<QDotNetRef>::Parameter, QDotNetOutbound<QDotNetRef>::Parameter,
                      QDotNetOutbound<QDotNetArray<QDotNetRef>>::Parameter });
        }
        return fnGet(obj, parameters);
    }

    QDotNetRef setValue(QDotNetRef obj, QDotNetRef value,
                        QDotNetArray<QDotNetRef> parameters = nullptr) const
    {
        if (!isValid())
            return nullptr;

        if (!fnSet.isValid()) {
            fnSet = adapter().resolveInstanceMethod(
                    *this, "SetValue",
                    { QDotNetInbound<QDotNetRef>::Parameter, QDotNetOutbound<QDotNetRef>::Parameter,
                      QDotNetOutbound<QDotNetRef>::Parameter,
                      QDotNetOutbound<QDotNetArray<QDotNetRef>>::Parameter });
        }
        return fnSet(obj, value, parameters);
    }

private:
    mutable QDotNetFunction<QDotNetRef, QDotNetRef, QDotNetArray<QDotNetRef>> fnGet;
    mutable QDotNetFunction<QDotNetRef, QDotNetRef, QDotNetRef, QDotNetArray<QDotNetRef>> fnSet;
};
