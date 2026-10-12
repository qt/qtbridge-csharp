// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetexception.h"
#include "qdotnetprofiler.h"

#ifdef DISABLE_QDOTNETSAFEMETHOD
#  define QDotNetSafeMethod QDotNetFunction
#else

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QList>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

// Calls a .NET method through a generated wrapper that catches its exceptions.
// The wrapper returns the result directly. The last argument is the address of
// a local exception handle initialized to nullptr. A caught exception's handle
// is written to that address; successful calls leave the handle unchanged.
template<typename T, typename... TArg>
class QDotNetSafeMethod
{
    Q_DOTNET_PROFILE_SCOPE(QDotNetSafeMethod);

    using FuncType = QDotNetFunction<T, TArg...>;
    using SafeFuncType = QDotNetFunction<T, QDotNetRef, TArg..., void *>;

public:
    QDotNetSafeMethod() = default;

    QDotNetSafeMethod(nullptr_t)
    { }

    QDotNetSafeMethod(void *funcPtr) : QDotNetSafeMethod(FuncType(funcPtr))
    { }

    QDotNetSafeMethod(FuncType func)
        : func(func)
    {
        Q_DOTNET_PROFILE_FUNC();

        const QList<QDotNetParameter> parameters
        {
            QDotNetInbound<T>::Parameter,
            QDotNetOutbound<QDotNetRef>::Parameter,
            QDotNetOutbound<TArg>::Parameter...,
            QDotNetOutbound<void *>::Parameter
        };
        safeFunc = QDotNetAdapter::instance().resolveSafeMethod(func.ptr(), parameters);
    }

    bool isValid() const { return func.isValid(); }

    typename QDotNetInbound<T>::TargetType invoke(QDotNetOutbound<QDotNetRef>::SourceType obj,
        typename QDotNetOutbound<TArg>::SourceType... arg) const
    {
        Q_DOTNET_PROFILE_FUNC();

        const void *exceptionHandle = nullptr;
        auto result = safeFunc(obj, arg..., &exceptionHandle);
        if (exceptionHandle)
            throw QDotNetException(exceptionHandle);
        return result;
    }

    typename QDotNetInbound<T>::TargetType invoke(nullptr_t,
        typename QDotNetOutbound<TArg>::SourceType... arg) const
    {
        Q_DOTNET_PROFILE_FUNC();

        const void *exceptionHandle = nullptr;
        auto result = safeFunc(nullptr, arg..., &exceptionHandle);
        if (exceptionHandle)
            throw QDotNetException(exceptionHandle);
        return result;
    }

private:
    FuncType func = nullptr;
    SafeFuncType safeFunc = nullptr;
};

template<typename... TArg>
class QDotNetSafeMethod<void, TArg...>
{
    using FuncType = QDotNetFunction<void, TArg...>;
    using SafeFuncType = QDotNetFunction<void, QDotNetRef, TArg..., void *>;

public:
    QDotNetSafeMethod() = default;

    QDotNetSafeMethod(nullptr_t)
    { }

    QDotNetSafeMethod(void *funcPtr) : QDotNetSafeMethod(FuncType(funcPtr))
    { }

    QDotNetSafeMethod(FuncType func)
        : func(func)
    {
        Q_DOTNET_PROFILE_FUNC();

        const QList<QDotNetParameter> parameters
        {
            QDotNetInbound<void>::Parameter,
            QDotNetOutbound<QDotNetRef>::Parameter,
            QDotNetOutbound<TArg>::Parameter...,
            QDotNetOutbound<void *>::Parameter
        };
        safeFunc = QDotNetAdapter::instance().resolveSafeMethod(func.ptr(), parameters);
    }

    bool isValid() const { return func.isValid(); }

    void invoke(QDotNetOutbound<QDotNetRef>::SourceType obj,
        typename QDotNetOutbound<TArg>::SourceType... arg) const
    {
        Q_DOTNET_PROFILE_FUNC();

        const void *exceptionHandle = nullptr;
        safeFunc(obj, arg..., &exceptionHandle);
        if (exceptionHandle)
            throw QDotNetException(exceptionHandle);
    }

    void invoke(nullptr_t, typename QDotNetOutbound<TArg>::SourceType... arg) const
    {
        Q_DOTNET_PROFILE_FUNC();

        const void *exceptionHandle = nullptr;
        safeFunc(nullptr, arg..., &exceptionHandle);
        if (exceptionHandle)
            throw QDotNetException(exceptionHandle);
    }

private:
    FuncType func = nullptr;
    SafeFuncType safeFunc = nullptr;
};

#endif // !defined(DISABLE_QDOTNETSAFEMETHOD)
