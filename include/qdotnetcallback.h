// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetfunction.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QMap>
#include <QList>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

template<typename T>
struct QDotNetCallbackArg : public QDotNetInbound<T> {};

template<typename T>
struct QDotNetCallbackReturn : public QDotNetOutbound<T> {};

class QDotNetCallbackBase
{
protected:
    QDotNetCallbackBase() = default;
public:
    virtual ~QDotNetCallbackBase() = default;
};

template<typename TResult, typename... TArg>
class QDotNetCallback : public QDotNetCallbackBase
{
public:
    using FunctionType = std::function<TResult(void *, TArg... arg)>;
    using CleanUpType = std::function<void(TResult *)>;

    using OutboundType = typename QDotNetCallbackReturn<TResult>::OutboundType;
    using Delegate = OutboundType(QDOTNETFUNCTION_CALLTYPE *)(
        QDotNetCallback *callback, quint64 key,
        void *data, typename QDotNetCallbackArg<TArg>::InboundType...);

    using CleanUp = void(QDOTNETFUNCTION_CALLTYPE *)(QDotNetCallback *callback, quint64 key);

    QDotNetCallback(FunctionType fnCallback, CleanUpType fnCleanUp = nullptr)
        : fnCallback(fnCallback), fnCleanUp(fnCleanUp)
    {}

    ~QDotNetCallback() override = default;

    static Delegate delegate()
    {
        return callbackDelegate;
    }

    static CleanUp cleanUp()
    {
        return callbackCleanUp;
    }

protected:
    quint64 activeKey() const
    {
        return activeKeys.isEmpty() ? 0 : activeKeys.last();
    }

private:
    struct Box
    {
        TResult returnValue;
        Box(TResult &&ret) : returnValue(std::move(ret)) {}
    };
    QMap<quint64, Box *> boxes;

    static OutboundType QDOTNETFUNCTION_CALLTYPE callbackDelegate(
        QDotNetCallback *callback, quint64 key,
        void *data, typename QDotNetCallbackArg<TArg>::InboundType... arg)
    {
        callback->activeKeys.append(key);
        Box *box = callback->boxes[key] = new Box(
            callback->fnCallback(data, QDotNetCallbackArg<TArg>::convert(arg)...));
        callback->activeKeys.removeLast();
        return QDotNetCallbackReturn<TResult>::convert(box->returnValue);
    }

    static void QDOTNETFUNCTION_CALLTYPE callbackCleanUp(QDotNetCallback *callback, quint64 key)
    {
        if (const Box *box = callback->boxes.take(key)) {
            if (callback->fnCleanUp)
                callback->fnCleanUp(const_cast<std::remove_const_t<TResult*>>(&(box->returnValue)));
            delete box;
        }
    }

    FunctionType fnCallback = nullptr;
    CleanUpType fnCleanUp = nullptr;
    QList<quint64> activeKeys;
};

template<typename... TArg>
class QDotNetCallback<void, TArg...> : public QDotNetCallbackBase
{
public:
    using FunctionType = std::function<void(void *, TArg... arg)>;
    using CleanUpType = nullptr_t;

    using Delegate = void(QDOTNETFUNCTION_CALLTYPE *)(
        QDotNetCallback *callback, quint64 key,
        void *data, typename QDotNetCallbackArg<TArg>::InboundType...);

    using CleanUp = nullptr_t;

    QDotNetCallback(FunctionType fnCallback, CleanUpType fnCleanUp = nullptr)
        : fnCallback(fnCallback)
    {}

    ~QDotNetCallback() override = default;

    static Delegate delegate()
    {
        return callbackDelegate;
    }

    static CleanUp cleanUp()
    {
        return nullptr;
    }

protected:
    quint64 activeKey() const
    {
        return activeKeys.isEmpty() ? 0 : activeKeys.last();
    }

private:
    static void QDOTNETFUNCTION_CALLTYPE callbackDelegate(
        QDotNetCallback *callback, quint64 key,
        void *data, typename QDotNetCallbackArg<TArg>::InboundType... arg)
    {
        callback->activeKeys.append(key);
        callback->fnCallback(data, QDotNetCallbackArg<TArg>::convert(arg)...);
        callback->activeKeys.removeLast();
    }

    FunctionType fnCallback = nullptr;
    QList<quint64> activeKeys;
};
