// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetsafemethod.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QString>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

class QDotNetType : public QDotNetRef
{
public:
    static inline const QString &AssemblyQualifiedName = QStringLiteral("System.Type");

    QDotNetType(const void *typeRef = nullptr)
        : QDotNetRef(typeRef)
    {}

    QDotNetType(const QDotNetRef &cpySrc)
        : QDotNetRef(adapter().addObjectRef(&cpySrc))
    {}

    QDotNetType &operator=(const QDotNetRef &cpySrc)
    {
        QDotNetRef::operator=(cpySrc);
        return *this;
    }

    QDotNetType(QDotNetRef &&movSrc) noexcept
        : QDotNetRef(std::move(movSrc))
    {}

    QDotNetType &operator=(QDotNetRef &&movSrc) noexcept
    {
        QDotNetRef::operator=(std::move(movSrc));
        return *this;
    }

    QDotNetRef module() const
    {
        if (!isValid())
            return nullptr;
        if (!fnModule.isValid()) {
            const QList<QDotNetParameter> parameters{ QDotNetInbound<QDotNetRef>::Parameter };
            fnModule = adapter().resolveInstanceMethod(*this, "get_Module", parameters);
        }
        return fnModule();
    }

    QString assemblyQualifiedName() const
    {
        if (!isValid())
            return QStringLiteral("");
        if (!fnAssemblyQualifiedName.isValid()) {
            fnAssemblyQualifiedName = adapter().resolveInstanceMethod(*this,
                "get_AssemblyQualifiedName", { UnmanagedType::LPWStr });
            strFullName = fnAssemblyQualifiedName();
        }
        return strFullName;
    }

    QString fullName() const
    {
        if (!isValid())
            return QStringLiteral("");
        if (!fnFullName.isValid()) {
            fnFullName = adapter().resolveInstanceMethod(*this,
                "get_FullName", { UnmanagedType::LPWStr });
            strFullName = fnFullName();
        }
        return strFullName;
    }

    static QDotNetType typeOf(const QString &typeName)
    {
        QDotNetFunction<QDotNetType, QString> fnGetType;
        return staticMethod(AssemblyQualifiedName, "GetType", fnGetType).invoke(nullptr, typeName);
    }

    template<typename T>
    static QDotNetType typeOf()
    {
        return typeOf(T::AssemblyQualifiedName);
    }

    template<typename T>
    static QDotNetFunction<T> staticFieldGet(const QString &typeName,
        const QString &fieldName)
    {
        const QList<QDotNetParameter> parameters
        {
            QDotNetInbound<T>::Parameter
        };
        return adapter().resolveStaticFieldGet(typeName, fieldName, parameters);
    }

    template<typename T>
    static QDotNetFunction<T> &staticFieldGet(const QString &typeName,
        const QString &fieldName, QDotNetFunction<T> &func)
    {
        if (!func.isValid())
            func = staticFieldGet<T>(typeName, fieldName);
        return func;
    }

    template<typename T>
    QDotNetFunction<T> staticFieldGet(const QString &fieldName) const
    {
        return staticFieldGet<T>(assemblyQualifiedName(), fieldName);
    }

    template<typename T>
    QDotNetFunction<T> &staticFieldGet(
        const QString &fieldName, QDotNetFunction<T> &func) const
    {
        if (!func.isValid())
            func = staticFieldGet<T>(fieldName);
        return func;
    }

    template<typename T>
    static QDotNetFunction<void, T> staticFieldSet(const QString &typeName,
        const QString &fieldName)
    {
        const QList<QDotNetParameter> parameters
        {
            QDotNetInbound<void>::Parameter,
            QDotNetOutbound<T>::Parameter
        };
        return adapter().resolveStaticFieldSet(typeName, fieldName, parameters);
    }

    template<typename T>
    static QDotNetFunction<void, T> &staticFieldSet(const QString &typeName,
        const QString &fieldName, QDotNetFunction<void, T> &func)
    {
        if (!func.isValid())
            func = staticFieldSet<T>(typeName, fieldName);
        return func;
    }

    template<typename TResult, typename ...TArg>
    static QDotNetFunction<TResult, TArg...> staticMethod(const QString &typeName,
        const QString &methodName)
    {
        const QList<QDotNetParameter> parameters
        {
            QDotNetInbound<TResult>::Parameter,
            QDotNetOutbound<TArg>::Parameter...
        };
        return adapter().resolveStaticMethod(typeName, methodName, parameters);
    }

    template<typename TResult, typename ...TArg>
    static QDotNetFunction<TResult, TArg...> &staticMethod(const QString &typeName,
        const QString &methodName, QDotNetFunction<TResult, TArg...> &func)
    {
        if (!func.isValid())
            func = staticMethod<TResult, TArg...>(typeName, methodName);
        return func;
    }

    template<typename TResult, typename ...TArg>
    static QDotNetSafeMethod<TResult, TArg...> &staticMethod(const QString &typeName,
        const QString &methodName, QDotNetSafeMethod<TResult, TArg...> &func)
    {
        if (!func.isValid())
            func = staticMethod<TResult, TArg...>(typeName, methodName);
        return func;
    }

    template<typename TResult, typename ...TArg>
    QDotNetFunction<TResult, TArg...> staticMethod(const QString &methodName) const
    {
        return staticMethod<TResult, TArg...>(assemblyQualifiedName(), methodName);
    }

    template<typename TResult, typename ...TArg>
    QDotNetFunction<TResult, TArg...> &staticMethod(const QString &methodName,
        QDotNetFunction<TResult, TArg...> &func) const
    {
        if (!func.isValid())
            func = staticMethod<TResult, TArg...>(methodName);
        return func;
    }

    template<typename TResult, typename ...TArg>
    QDotNetSafeMethod<TResult, TArg...> &staticMethod(const QString &methodName,
        QDotNetSafeMethod<TResult, TArg...> &func) const
    {
        if (!func.isValid())
            func = staticMethod<TResult, TArg...>(methodName);
        return func;
    }

    template<typename T, typename ...TArg>
    static QDotNetFunction<T, TArg...> constructor(const QString &typeName)
    {
        const QList<QDotNetParameter> parameters
        {
            QDotNetParameter(typeName, UnmanagedType::ObjectRef),
            QDotNetOutbound<TArg>::Parameter...
        };
        return adapter().resolveConstructor(parameters);
    }

    template<typename T, typename ...TArg>
    static QDotNetFunction<T, TArg...> &constructor(const QString &typeName,
        QDotNetFunction<T, TArg...> &ctor)
    {
        if (!ctor.isValid())
            ctor = constructor<T, TArg...>(typeName);
        return ctor;
    }

    template<typename T, typename ...TArg>
    static QDotNetSafeMethod<T, TArg...> &constructor(const QString &typeName,
        QDotNetSafeMethod<T, TArg...> &ctor)
    {
        if (!ctor.isValid())
            ctor = constructor<T, TArg...>(typeName);
        return ctor;
    }

    template<typename T, typename ...TArg>
    QDotNetFunction<T, TArg...> constructor() const
    {
        return constructor<T, TArg...>(assemblyQualifiedName());
    }

    template<typename T, typename ...TArg>
    QDotNetFunction<T, TArg...> &constructor(QDotNetFunction<T, TArg...> &ctor) const
    {
        return constructor(assemblyQualifiedName(), ctor);
    }

    template<typename T, typename ...TArg>
    QDotNetFunction<T, TArg...> &constructor(QDotNetSafeMethod<T, TArg...> &ctor) const
    {
        return constructor(assemblyQualifiedName(), ctor);
    }

    void freeTypeRef()
    {
        freeTypeRef(assemblyQualifiedName());
    }

    static void freeTypeRef(const QString &typeName)
    {
        adapter().freeTypeRef(typeName);
    }

    template<typename T>
    static void freeTypeRef()
    {
        freeTypeRef(T::AssemblyQualifiedName);
    }

    bool isAssignableFrom(QDotNetType c) const
    {
        if (!c.isValid())
            return false;
        if (!fnIsAssignableFrom.isValid()) {
            fnIsAssignableFrom = adapter().resolveInstanceMethod(*this, "IsAssignableFrom",
                { UnmanagedType::Bool, QStringLiteral("System.Type") });
            if (!fnIsAssignableFrom.isValid())
                return false;
        }
        return fnIsAssignableFrom(c);
    }

    template<typename T>
    bool isAssignableFrom() const
    {
        return isAssignableFrom(typeOf<T>());
    }

    bool isAssignableTo(QDotNetType c) const
    {
        if (!c.isValid())
            return false;
        if (!fnIsAssignableTo.isValid()) {
            fnIsAssignableTo = adapter().resolveInstanceMethod(*this, "IsAssignableTo",
                { UnmanagedType::Bool, QStringLiteral("System.Type") });
            if (!fnIsAssignableTo.isValid())
                return false;
        }
        return fnIsAssignableTo(c);
    }

    template<typename T>
    bool isAssignableTo() const
    {
        return isAssignableTo(typeOf<T>());
    }

    bool is(const QDotNetType &c) const
    {
        return equals(c);
    }

    template<typename T>
    bool is() const
    {
        return is(typeOf<T>());
    }

private:
    mutable QDotNetFunction<QDotNetRef> fnModule;
    mutable QDotNetFunction<QString> fnAssemblyQualifiedName;
    mutable QDotNetFunction<QString> fnFullName;
    mutable QString strFullName;
    mutable QDotNetFunction<bool, QDotNetRef> fnIsAssignableFrom;
    mutable QDotNetFunction<bool, QDotNetRef> fnIsAssignableTo;
};

namespace QtDotNet
{
    template<typename T, typename... TArg>
    T call(const QString &type, const QString &method, TArg... arg)
    {
        return QDotNetType::staticMethod<T, TArg...>(type, method).invoke(nullptr, arg...);
    }
}
