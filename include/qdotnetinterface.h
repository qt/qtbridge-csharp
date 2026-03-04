// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetref.h"
#include "qdotnetcallback.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QList>
#include <QString>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

class QDotNetInterface : public QDotNetRef
{
public:
    QDotNetInterface(const QString &interfaceName, void *data = nullptr, void *cleanUp = nullptr)
        : QDotNetRef(adapter().addInterfaceProxy(interfaceName, data, cleanUp))
    {}

    QDotNetInterface(const void *objectRef = nullptr)
        : QDotNetRef(objectRef)
    {}

    QDotNetInterface(const QDotNetInterface &cpySrc)
        : QDotNetRef(cpySrc)
    {}

    QDotNetInterface &operator =(const QDotNetInterface &cpySrc)
    {
        QDotNetRef::operator=(cpySrc);
        return *this;
    }

    QDotNetInterface(QDotNetInterface &&movSrc) noexcept
        : QDotNetRef(std::move(movSrc))
    {}

    QDotNetInterface &operator=(QDotNetInterface &&movSrc) noexcept
    {
        QDotNetRef::operator=(std::move(movSrc));
        return *this;
    }

    template<typename TFn>
    static void *asHandle(TFn fn)
    {
        // setInterfaceMethod stores callback handles as void*.
        // GCC is stricter than MSVC here, so make the function-pointer conversion explicit.
        return reinterpret_cast<void *>(fn);
    }

    static void *asHandle(std::nullptr_t)
    {
        // Keep nullptr cleanup callbacks well-typed across compilers.
        return nullptr;
    }

    template<typename T>
    T *dataAs()
    {
        if (!fnDataPtr.isValid()) {
            const QList<QDotNetParameter> parameters
            {
                QDotNetInbound<void *>::Parameter
            };
            fnDataPtr = adapter().resolveInstanceMethod(*this, "get_Data", parameters);
        }
        return reinterpret_cast<T *>(fnDataPtr());
    }

    virtual ~QDotNetInterface() override
    {
        if (!isValid())
            return;
        for (const QDotNetCallbackBase *callback : callbacks)
            delete callback;
        callbacks.clear();
    }

    template<typename TResult, typename... TArg>
    void setCallback(const QString &methodName,
        typename QDotNetCallback<TResult, TArg...>::FunctionType function,
        typename QDotNetCallback<TResult, TArg...>::CleanUpType cleanUp = nullptr)
    {
        auto *callback = new QDotNetCallback<TResult, TArg...>(function, cleanUp);
        callbacks.append(callback);

        const QList<QDotNetParameter> parameters
        {
            QDotNetCallbackReturn<TResult>::Parameter,
            UnmanagedType::SysInt,
            UnmanagedType::U8,
            UnmanagedType::SysInt,
            QDotNetCallbackArg<TArg>::Parameter...
        };

        adapter().setInterfaceMethod(
            *this, methodName, parameters,
            asHandle(callback->delegate()), asHandle(callback->cleanUp()), callback);
    }

private:
    QList<QDotNetCallbackBase *> callbacks;
    QDotNetFunction<void *> fnDataPtr = nullptr;
};

template<typename T>
struct QDotNetTypeOf<T, std::enable_if_t<std::is_base_of_v<QDotNetInterface, T>>>
{
    static inline const QString TypeName = T::AssemblyQualifiedName;
    static inline UnmanagedType MarshalAs = UnmanagedType::ObjectRef;
};

template<typename T>
struct QDotNetNativeInterface : public QDotNetInterface
{
    QDotNetNativeInterface(const void *objectRef = nullptr)
        : QDotNetInterface(objectRef)
    {
    }

    QDotNetNativeInterface(const QString &interfaceName, T *data, bool doCleanUp = true)
        : QDotNetInterface(interfaceName, data, asHandle(doCleanUp ? cleanUp : nullptr))
    {
    }

    T *data()
    {
        return dataAs<T>();
    }

    operator T&()
    {
        return *data();
    }

    static void QDOTNETFUNCTION_CALLTYPE cleanUp(void *data)
    {
        if (data)
            delete reinterpret_cast<T *>(data);
    }
};
