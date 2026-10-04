// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only
#pragma once

#include "qdotnetarray.h"
#include "qdotnetcallback.h"
#include "qdotnetobject.h"
#include "qdotnetparameter.h"
#include "qdotnetreflection.h"
#include "qdotnettype.h"
#include "qdotnetprofiler.h"

#include <functional>

#ifdef __GNUC__
#  pragma GCC diagnostic push
#  pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QChar>
#include <QDateTime>
#include <QDebug>
#include <QJSEngine>
#include <QJSValue>
#include <QMetaObject>
#include <QModelIndex>
#include <QObject>
#include <QPointer>
#include <QQmlEngine>
#include <QString>
#include <QThread>
#include <QUrl>
#include <QtTypes>
#ifdef __GNUC__
#  pragma GCC diagnostic pop
#endif

class QDotNetConvert
{
    Q_DOTNET_PROFILE_SCOPE(QDotNetConvert);

public:
    static inline const QString TypeName =
            QStringLiteral("Qt.Bridge.ValueConverter, Qt.Bridge.CSharp.Api");

    using FnObjectDispatch = std::function<QObject *(QDotNetRef &, const QObject *)>;

    static void setDispatch(FnObjectDispatch fn)
    {
        Q_DOTNET_PROFILE_FUNC();

        fnObjectDispatch = fn;
    }

    static QObject *objectDispatch(QDotNetRef &dotnetObj, const QObject *context = nullptr)
    {
        Q_DOTNET_PROFILE_FUNC();

        if (!fnObjectDispatch)
            return nullptr;
        return fnObjectDispatch(dotnetObj, context);
    }

    static QDotNetRef fromBoolean(bool value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, bool> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromBoolean", fn);
        return fn(value);
    }

    static QDotNetRef fromSByte(qint8 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, qint8> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromSByte", fn);
        return fn(value);
    }

    static QDotNetRef fromByte(quint8 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, quint8> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromByte", fn);
        return fn(value);
    }

    static QDotNetRef fromInt16(qint16 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, qint16> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromInt16", fn);
        return fn(value);
    }

    static QDotNetRef fromUInt16(quint16 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, quint16> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromUInt16", fn);
        return fn(value);
    }

    static QDotNetRef fromInt32(qint32 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, qint32> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromInt32", fn);
        return fn(value);
    }

    static QDotNetRef fromUInt32(quint32 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, quint32> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromUInt32", fn);
        return fn(value);
    }

    static QDotNetRef fromInt64(qint64 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, qint64> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromInt64", fn);
        return fn(value);
    }

    static QDotNetRef fromUInt64(quint64 value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, quint64> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromUInt64", fn);
        return fn(value);
    }

    static QDotNetRef fromSingle(float value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, float> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromSingle", fn);
        return fn(value);
    }

    static QDotNetRef fromDouble(double value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, double> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromDouble", fn);
        return fn(value);
    }

    static QDotNetRef fromDateTime(const QDateTime &value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, QDateTime> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromDateTime", fn);
        return fn(value);
    }

    static QDotNetRef fromUri(const QUrl &value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, QUrl> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromUri", fn);
        return fn(value);
    }

    static QDotNetRef fromModelIndex(const QModelIndex &value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, QModelIndex> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromModelIndex", fn);
        return fn(value);
    }

    static QDotNetRef fromChar(QChar value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, QChar> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromChar", fn);
        return fn(value);
    }

    static QDotNetRef fromString(const QString &value)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetRef, QString> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "FromString", fn);
        return fn(value);
    }

    static bool isBoolean(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsBoolean", fn);
        return fn(obj);
    }

    static bool isSByte(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsSByte", fn);
        return fn(obj);
    }

    static bool isByte(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsByte", fn);
        return fn(obj);
    }

    static bool isInt16(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsInt16", fn);
        return fn(obj);
    }

    static bool isUInt16(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsUInt16", fn);
        return fn(obj);
    }

    static bool isInt32(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsInt32", fn);
        return fn(obj);
    }

    static bool isUInt32(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsUInt32", fn);
        return fn(obj);
    }

    static bool isInt64(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsInt64", fn);
        return fn(obj);
    }

    static bool isUInt64(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsUInt64", fn);
        return fn(obj);
    }

    static bool isSingle(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsSingle", fn);
        return fn(obj);
    }

    static bool isDouble(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsDouble", fn);
        return fn(obj);
    }

    static bool isDateTime(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsDateTime", fn);
        return fn(obj);
    }

    static bool isUri(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsUri", fn);
        return fn(obj);
    }

    static bool isModelIndex(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsModelIndex", fn);
        return fn(obj);
    }

    static bool isChar(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsChar", fn);
        return fn(obj);
    }

    static bool isString(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsString", fn);
        return fn(obj);
    }

    static bool toBoolean(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToBoolean", fn);
        return fn(obj);
    }

    static qint8 toSByte(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<qint8, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToSByte", fn);
        return fn(obj);
    }

    static quint8 toByte(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<quint8, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToByte", fn);
        return fn(obj);
    }

    static qint16 toInt16(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<qint16, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToInt16", fn);
        return fn(obj);
    }

    static quint16 toUInt16(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<quint16, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToUInt16", fn);
        return fn(obj);
    }

    static qint32 toInt32(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<qint32, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToInt32", fn);
        return fn(obj);
    }

    static quint32 toUInt32(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<quint32, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToUInt32", fn);
        return fn(obj);
    }

    static qint64 toInt64(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<qint64, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToInt64", fn);
        return fn(obj);
    }

    static quint64 toUInt64(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<quint64, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToUInt64", fn);
        return fn(obj);
    }

    static float toSingle(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<float, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToSingle", fn);
        return fn(obj);
    }

    static double toDouble(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<double, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToDouble", fn);
        return fn(obj);
    }

    static QDateTime toDateTime(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDateTime, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToDateTime", fn);
        return fn(obj);
    }

    static QUrl toUri(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QUrl, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToUri", fn);
        return fn(obj);
    }

    static QModelIndex toModelIndex(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QModelIndex, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToModelIndex", fn);
        return fn(obj);
    }

    static QChar toChar(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QChar, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToChar", fn);
        return fn(obj);
    }

    static QString toString(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QString, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToString", fn);
        return fn(obj);
    }

    static bool isConvertible(const QDotNetRef &t)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<bool, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "IsConvertible", fn);
        return fn(t);
    }

    static QDotNetArray<QDotNetRef> toArray(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<QDotNetArray<QDotNetRef>, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ToArray", fn);
        return fn(obj);
    }

    static QDotNetObject fromVariant(const QVariant &value)
    {
        Q_DOTNET_PROFILE_FUNC();

        switch (value.typeId()) {
        case QMetaType::Bool:
            return fromBoolean(value.toBool());
        case QMetaType::Char:
        case QMetaType::SChar:
            return fromSByte((qint8)value.toInt());
        case QMetaType::UChar:
            return fromByte((quint8)value.toInt());
        case QMetaType::Short:
            return fromInt16((qint16)value.toInt());
        case QMetaType::UShort:
            return fromUInt16((quint16)value.toInt());
        case QMetaType::Int:
        case QMetaType::Long:
            return fromInt32(value.toInt());
        case QMetaType::UInt:
        case QMetaType::ULong:
            return fromUInt32(value.toUInt());
        case QMetaType::LongLong:
            return fromInt64(value.toLongLong());
        case QMetaType::ULongLong:
            return fromUInt64(value.toULongLong());
        case QMetaType::Float:
            return fromSingle(value.toFloat());
        case QMetaType::Double:
            return fromDouble(value.toDouble());
        case QMetaType::QChar:
            return fromChar(value.toChar());
        case QMetaType::QString:
            return fromString(value.toString());
        case QMetaType::QDate:
        case QMetaType::QDateTime:
            return fromDateTime(value.toDateTime());
        case QMetaType::QUrl:
            return fromUri(value.toUrl());
        case QMetaType::QPersistentModelIndex:
        case QMetaType::QModelIndex:
            return fromModelIndex(value.toModelIndex());
        default:
            if (value.metaType().flags() & QMetaType::PointerToQObject) {
                auto *dnObj = asDotNetObject(value.value<QObject *>());
                if (dnObj && dnObj->isValid())
                    return *dnObj;
            }
        }
        return nullptr;
    }

    static int valueTypeId(const QDotNetRef &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        static QDotNetFunction<int, QDotNetRef> fn;
        if (!fn.isValid())
            QDotNetType::staticMethod(TypeName, "ValueTypeId", fn);
        return fn(obj);
    }

    static QVariant toVariant(QDotNetRef &obj, const QObject *context)
    {
        Q_DOTNET_PROFILE_FUNC();

        switch (valueTypeId(obj)) {
        case QMetaType::Bool:
            return QVariant::fromValue(toBoolean(obj));
        case QMetaType::SChar:
            return QVariant::fromValue(toSByte(obj));
        case QMetaType::UChar:
            return QVariant::fromValue(toByte(obj));
        case QMetaType::Short:
            return QVariant::fromValue(toInt16(obj));
        case QMetaType::UShort:
            return QVariant::fromValue(toUInt16(obj));
        case QMetaType::Int:
            return QVariant::fromValue(toInt32(obj));
        case QMetaType::UInt:
            return QVariant::fromValue(toUInt32(obj));
        case QMetaType::LongLong:
            return QVariant::fromValue(toInt64(obj));
        case QMetaType::ULongLong:
            return QVariant::fromValue(toUInt64(obj));
        case QMetaType::Float:
            return QVariant::fromValue(toSingle(obj));
        case QMetaType::Double:
            return QVariant::fromValue(toDouble(obj));
        case QMetaType::QDateTime:
            return QVariant::fromValue(toDateTime(obj));
        case QMetaType::QChar:
            return QVariant::fromValue(toChar(obj));
        case QMetaType::QString:
            return QVariant::fromValue(toString(obj));
        case QMetaType::QUrl:
            return QVariant::fromValue(toUri(obj));
        case QMetaType::QModelIndex:
            return QVariant::fromValue(toModelIndex(obj));
        case QMetaType::QObjectStar:
            if (QObject *qObj = objectDispatch(obj, context))
                return QVariant::fromValue(qObj);
        }
        return {};
    }

    static const QDotNetObject *asDotNetObject(QObject *qObj)
    {
        Q_DOTNET_PROFILE_FUNC();

        if (!qObj)
            return nullptr;

        // Standard moc-preprocessed types and dynamic objects can skip metaObject(),
        // indexOfMethod("asDotNetObject()") and method(...).invoke(...), and return
        // the backing QDotNetObject directly.
        if (void *ptr = qObj->qt_metacast(QDotNetObject::ClassName))
            return reinterpret_cast<const QDotNetObject *>(ptr);

        auto *mObj = qObj->metaObject();
        if (!mObj)
            return nullptr;
        int mIdx = mObj->indexOfMethod("asDotNetObject()");
        if (mIdx == -1)
            return nullptr;
        const QDotNetObject *dnObj = nullptr;
        if (!mObj->method(mIdx).invoke(qObj, Q_RETURN_ARG(const QDotNetObject *, dnObj)))
            return nullptr;
        return dnObj;
    }

    template <typename TFn>
    static void *asHandle(TFn fn)
    {
        return reinterpret_cast<void *>(fn);
    }

    static void *asHandle(std::nullptr_t) { return nullptr; }

    template <typename TValue>
    static QJSValue toScriptValue(QJSEngine *engine, TValue &value,
                                  const QObject *context = nullptr)
    {
        Q_DOTNET_PROFILE_FUNC();

        if (!engine)
            return {};
        if constexpr (std::is_base_of_v<QDotNetRef, TValue>) {
            QVariant variant = toVariant(value, context);
            if (variant.metaType().flags() & QMetaType::PointerToQObject)
                return engine->newQObject(variant.value<QObject *>());
            return engine->toScriptValue(variant);
        }
        return engine->toScriptValue(QVariant::fromValue(value));
    }

    template <typename TValue>
    static TValue fromScriptResult(const QJSValue &value)
    {
        Q_DOTNET_PROFILE_FUNC();

        if constexpr (std::is_base_of_v<QDotNetRef, TValue>) {
            QDotNetObject obj = value.isQObject()
                    ? fromVariant(QVariant::fromValue(value.toQObject()))
                    : fromVariant(value.toVariant());
            if constexpr (std::is_same_v<TValue, QDotNetObject>)
                return obj;
            return obj.cast<TValue>();
        }
        return value.toVariant().template value<TValue>();
    }

    static void reportInvokeFailure(const QString &delegateTypeName, const QString &message)
    {
        Q_DOTNET_PROFILE_FUNC();

        qCritical() << "Qt/.NET delegate callback failed for" << delegateTypeName << ":" << message;
    }

    static void reportScriptError(const QString &delegateTypeName, const QJSValue &error)
    {
        Q_DOTNET_PROFILE_FUNC();

        qCritical().nospace() << "Qt/.NET delegate callback threw for " << delegateTypeName << ": "
                              << error.toString();
        if (error.hasProperty("stack")) {
            const QString stack = error.property("stack").toString();
            if (!stack.isEmpty())
                qCritical().noquote() << stack;
        }
    }

    template <typename TResult, typename... TArg>
    class ScriptDelegateContext final : public QDotNetCallback<TResult, TArg...>
    {
    public:
        using Error = const QChar *(QDOTNETFUNCTION_CALLTYPE *)(void *, quint64);
        using CleanUp = typename QDotNetCallback<TResult, TArg...>::CleanUp;

        ScriptDelegateContext(const QString &delegateTypeName, const QJSValue &function,
                              QQmlEngine *engine, QObject *context)
            : QDotNetCallback<TResult, TArg...>([](void *data, TArg... arg) -> TResult {
                  return reinterpret_cast<ScriptDelegateContext *>(data)->invoke(arg...);
              }),
              delegateTypeName(delegateTypeName),
              function(function),
              engine(engine),
              context(context)
        {
        }

        static void QDOTNETFUNCTION_CALLTYPE deleteSelf(void *data)
        {
            Q_DOTNET_PROFILE_FUNC();

            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            // Engine already gone: QJSValue is no longer referenced, safe to delete
            // from any thread.
            if (!self->engine) {
                delete self;
                return;
            }
            // QJSValue has thread affinity; destroy on the engine thread.
            if (QThread::currentThread() == self->engine->thread()) {
                delete self;
                return;
            }
            // GC finalizer runs off the engine thread; post the deletion so the
            // QJSValue destructor runs on the correct thread.
            QMetaObject::invokeMethod(self->engine, [self] { delete self; }, Qt::QueuedConnection);
        }

        static CleanUp cleanUp() { return cleanUpCallback; }

        static Error error() { return errorCallback; }

    private:
        TResult invoke(TArg... arg)
        {
            Q_DOTNET_PROFILE_FUNC();

            if (!engine || !function.isCallable())
                return QtDotNet::null<TResult>();
            if (QThread::currentThread() == engine->thread())
                return invokeNow(arg...);

            TResult result = QtDotNet::null<TResult>();
            // NOTE: BlockingQueuedConnection will deadlock if the calling thread also
            // processes Qt events (e.g. is the main/UI thread). Delegates must only be
            // invoked from non-event-loop threads or a dedicated worker thread.
            const bool ok = QMetaObject::invokeMethod(
                    engine, [this, &result, arg...]() mutable { result = invokeNow(arg...); },
                    Qt::BlockingQueuedConnection);
            if (!ok) {
                recordErrorMessage("queued invoke on QQmlEngine failed");
                QDotNetConvert::reportInvokeFailure(delegateTypeName,
                                                    "queued invoke on QQmlEngine failed");
            }
            return result;
        }

        TResult invokeNow(TArg... arg)
        {
            Q_DOTNET_PROFILE_FUNC();

            QJSValueList args{ QDotNetConvert::toScriptValue(engine, arg, context)... };
            const QJSValue result = function.call(args);
            if (result.isError()) {
                recordErrorValue(result);
                QDotNetConvert::reportScriptError(delegateTypeName, result);
                return QtDotNet::null<TResult>();
            }
            return QDotNetConvert::fromScriptResult<TResult>(result);
        }

        void recordErrorMessage(const QString &message)
        {
            Q_DOTNET_PROFILE_FUNC();

            errors[QDotNetCallback<TResult, TArg...>::activeKey()] = message;
        }

        void recordErrorValue(const QJSValue &error)
        {
            Q_DOTNET_PROFILE_FUNC();

            QString message = error.toString();
            if (error.hasProperty("stack")) {
                const QString stack = error.property("stack").toString();
                if (!stack.isEmpty())
                    message += QStringLiteral("\n") + stack;
            }
            recordErrorMessage(message);
        }

        static void QDOTNETFUNCTION_CALLTYPE
        cleanUpCallback(QDotNetCallback<TResult, TArg...> *data, quint64 key)
        {
            Q_DOTNET_PROFILE_FUNC();

            auto *self = static_cast<ScriptDelegateContext *>(data);
            self->errors.remove(key);
            if (auto cleanUp = QDotNetCallback<TResult, TArg...>::cleanUp())
                cleanUp(self, key);
        }

        static const QChar *QDOTNETFUNCTION_CALLTYPE errorCallback(void *data, quint64 key)
        {
            Q_DOTNET_PROFILE_FUNC();

            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            const auto it = self->errors.constFind(key);
            return it == self->errors.cend() ? nullptr : it.value().constData();
        }

        QString delegateTypeName;
        QJSValue function;
        QPointer<QQmlEngine> engine;
        QPointer<QObject> context;
        QMap<quint64, QString> errors;
    };

    template <typename... TArg>
    class ScriptDelegateContext<void, TArg...> final : public QDotNetCallback<void, TArg...>
    {
    public:
        using Error = const QChar *(QDOTNETFUNCTION_CALLTYPE *)(void *, quint64);
        using CleanUp = void(QDOTNETFUNCTION_CALLTYPE *)(void *, quint64);

        ScriptDelegateContext(const QString &delegateTypeName, const QJSValue &function,
                              QQmlEngine *engine, QObject *context)
            : QDotNetCallback<void, TArg...>([](void *data, TArg... arg) {
                  reinterpret_cast<ScriptDelegateContext *>(data)->invoke(arg...);
              }),
              delegateTypeName(delegateTypeName),
              function(function),
              engine(engine),
              context(context)
        {
        }

        static void QDOTNETFUNCTION_CALLTYPE deleteSelf(void *data)
        {
            Q_DOTNET_PROFILE_FUNC();

            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            // Engine already gone: QJSValue is no longer referenced, safe to delete
            // from any thread.
            if (!self->engine) {
                delete self;
                return;
            }
            // QJSValue has thread affinity; destroy on the engine thread.
            if (QThread::currentThread() == self->engine->thread()) {
                delete self;
                return;
            }
            // GC finalizer runs off the engine thread; post the deletion so the
            // QJSValue destructor runs on the correct thread.
            QMetaObject::invokeMethod(self->engine, [self] { delete self; }, Qt::QueuedConnection);
        }

        static CleanUp cleanUp() { return cleanUpCallback; }

        static Error error() { return errorCallback; }

    private:
        void invoke(TArg... arg)
        {
            Q_DOTNET_PROFILE_FUNC();

            if (!engine || !function.isCallable())
                return;
            if (QThread::currentThread() == engine->thread()) {
                invokeNow(arg...);
                return;
            }

            // NOTE: BlockingQueuedConnection will deadlock if the calling thread also
            // processes Qt events (e.g. is the main/UI thread). Delegates must only be
            // invoked from non-event-loop threads or a dedicated worker thread.
            const bool ok = QMetaObject::invokeMethod(
                    engine, [this, arg...]() mutable { invokeNow(arg...); },
                    Qt::BlockingQueuedConnection);
            if (!ok) {
                recordErrorMessage("queued invoke on QQmlEngine failed");
                QDotNetConvert::reportInvokeFailure(delegateTypeName,
                                                    "queued invoke on QQmlEngine failed");
            }
        }

        void invokeNow(TArg... arg)
        {
            Q_DOTNET_PROFILE_FUNC();

            QJSValueList args{ QDotNetConvert::toScriptValue(engine, arg, context)... };
            const QJSValue result = function.call(args);
            if (result.isError()) {
                recordErrorValue(result);
                QDotNetConvert::reportScriptError(delegateTypeName, result);
            }
        }

        void recordErrorMessage(const QString &message)
        {
            Q_DOTNET_PROFILE_FUNC();

            errors[QDotNetCallback<void, TArg...>::activeKey()] = message;
        }

        void recordErrorValue(const QJSValue &error)
        {
            Q_DOTNET_PROFILE_FUNC();

            QString message = error.toString();
            if (error.hasProperty("stack")) {
                const QString stack = error.property("stack").toString();
                if (!stack.isEmpty())
                    message += QStringLiteral("\n") + stack;
            }
            recordErrorMessage(message);
        }

        static void QDOTNETFUNCTION_CALLTYPE cleanUpCallback(void *data, quint64 key)
        {
            Q_DOTNET_PROFILE_FUNC();

            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            self->errors.remove(key);
            // QDotNetCallback<void,...>::CleanUp is nullptr_t — no parent cleanup to call.
        }

        static const QChar *QDOTNETFUNCTION_CALLTYPE errorCallback(void *data, quint64 key)
        {
            Q_DOTNET_PROFILE_FUNC();

            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            const auto it = self->errors.constFind(key);
            return it == self->errors.cend() ? nullptr : it.value().constData();
        }

        QString delegateTypeName;
        QJSValue function;
        QPointer<QQmlEngine> engine;
        QPointer<QObject> context;
        QMap<quint64, QString> errors;
    };

    template <typename TDelegate, typename TResult, typename... TArg>
    static TDelegate fromScriptDelegate(const QString &delegateTypeName, const QJSValue &value,
                                        const QObject *context = nullptr)
    {
        Q_DOTNET_PROFILE_FUNC();

        if (!value.isCallable())
            return nullptr;
        auto *qContext = const_cast<QObject *>(context);
        auto *engine = qContext ? qmlEngine(qContext) : nullptr;
        if (!engine) {
            qWarning() << "Qt/.NET: cannot create delegate proxy for" << delegateTypeName
                       << ": no QML engine found for context";
            return nullptr;
        }

        auto *callback = new ScriptDelegateContext<TResult, TArg...>(delegateTypeName, value,
                                                                     engine, qContext);
        return TDelegate(QDotNetAdapter::instance().addDelegateProxy(
                delegateTypeName, TDelegate::SignatureParameters(), callback,
                asHandle(&ScriptDelegateContext<TResult, TArg...>::deleteSelf),
                asHandle(ScriptDelegateContext<TResult, TArg...>::delegate()),
                asHandle(ScriptDelegateContext<TResult, TArg...>::cleanUp()),
                asHandle(ScriptDelegateContext<TResult, TArg...>::error()), callback));
    }

    template <typename T>
    static T *as(QDotNetRef &obj, bool addRef, const QObject *context = nullptr)
    {
        Q_DOTNET_PROFILE_FUNC();

        static_assert(std::is_base_of_v<QObject, T>, "as<T> requires a QObject-derived type");
        if (!QDotNetType(obj.type()).isAssignableTo<T>())
            return nullptr;
        T *tObj = new T(obj.cast<T>(addRef));
        if (context
            && QJSEngine::objectOwnership(const_cast<QObject *>(context))
                    == QJSEngine::JavaScriptOwnership) {
            QJSEngine::setObjectOwnership(tObj, QJSEngine::JavaScriptOwnership);
        }
        return tObj;
    }

    template <typename T>
    static T *as(QObject *qObj)
    {
        Q_DOTNET_PROFILE_FUNC();

        if (!qObj)
            return nullptr;
        auto dnObj = const_cast<QDotNetObject *>(asDotNetObject(qObj));
        if (!dnObj || !dnObj->isValid())
            return nullptr;
        return as<T>(*dnObj, true, qObj);
    }

    template <typename T>
    static T *moveToHeap(T &obj, const QObject *context = nullptr)
    {
        Q_DOTNET_PROFILE_FUNC();

        static_assert(std::is_base_of_v<QObject, T> && std::is_base_of_v<QDotNetObject, T>,
                      "moveToHeap<T> requires a QObject- and QDotNetObject-derived type");
        auto *tObj = new T(std::move(obj));
        if (context
            && QJSEngine::objectOwnership(const_cast<QObject *>(context))
                    == QJSEngine::JavaScriptOwnership) {
            QJSEngine::setObjectOwnership(tObj, QJSEngine::JavaScriptOwnership);
        }
        return tObj;
    }

    template <typename T>
    struct Object
    {
        static_assert(std::is_base_of_v<QObject, T> && std::is_base_of_v<QDotNetObject, T>,
                      "Object<T> requires a QObject- and QDotNetObject-derived type");

        static T *null() { return nullptr; }

        static bool isValid(T *obj) { return obj && obj->isValid(); }

        template <typename V>
        static T *toValue(T *obj)
        {
            Q_DOTNET_PROFILE_FUNC();

            static_assert(std::is_same_v<V, T *>, "Value must be pointer to object type.");
            return obj;
        }

        template <typename V>
        static T *fromValue(T *obj)
        {
            Q_DOTNET_PROFILE_FUNC();

            static_assert(std::is_same_v<V, T>, "Value must be of the object type.");
            return obj;
        }
    };

private:
    static inline FnObjectDispatch fnObjectDispatch = nullptr;
};

// Explicit specialization must be at namespace scope (GCC/Clang are strict here).
template <>
struct QDotNetConvert::Object<QDotNetObject>
{
    static QVariant null() { return QVariant(); }

    static bool isValid(const QVariant &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        if (!obj.isValid())
            return false;
        if (obj.metaType().flags() & QMetaType::PointerToQObject) {
            if (const QDotNetObject *dnObj = QDotNetConvert::asDotNetObject(obj.value<QObject *>()))
                return dnObj->isValid();
            else
                return false;
        }
        return true;
    }

    template <typename V>
    static V toValue(const QVariant &obj)
    {
        Q_DOTNET_PROFILE_FUNC();

        return obj.value<V>();
    }

    template <typename V>
    static QVariant fromValue(V value)
    {
        Q_DOTNET_PROFILE_FUNC();

        return QVariant::fromValue(value);
    }
};
