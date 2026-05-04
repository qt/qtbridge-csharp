// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules
{
    using Extensions;
    using MetaFunctions;
    using static Placeholders;
    using static Traits;

    public class GenerateConvert : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo _)
        {
            var convertHppPath = "hpp/convert.h";
            var convertCppPath = "cpp/convert.cpp";

            var type = TypeOf(typeof(ValueConverter));

            var funcs = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(func => func.Name != "ToArray");

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += convertHppPath;
            sourceFiles += convertCppPath;

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var convertHpp = new FilePlaceholder(
                ConvertHeader, Root, $"{Root.MFn(Dir)}{convertHppPath}");
            convertHpp += $@"
#pragma once
#include <builtin_types.h>
#include <QDotNetCallback>
#include <QJSEngine>
#include <QJSValue>
#include <QMetaObject>
#include <QPointer>
#include <QQmlEngine>
#include <QThread>

struct Convert
{{
    static inline const QString TypeName = QStringLiteral(""{type.MFn(Src | Fqn)}"");
    static QDotNetType &type();
    {string.Join(@"
    ", funcs.Select(func => $@"{Wrap}
    static {(!func.ReturnType.IsObject() ? func.ReturnType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
{func.MFn(Name)}({string.Join(", ", func.GetParameters().Select(arg => $@"{Wrap}
    {(!arg.ParameterType.IsObject() ? arg.ParameterType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
    {arg.MFn(Name | Src)}"))});"))}
    static QDotNetArray<QDotNetObject> toArray(QDotNetObject obj);
    static QDotNetObject fromVariant(const QVariant &value);
    static QVariant toVariant(QDotNetObject obj, const QObject *context = nullptr);
    static const QDotNetObject *asDotNetObject(QObject *qObj);

    template<typename TFn>
    static void *asHandle(TFn fn)
    {{
        return reinterpret_cast<void *>(fn);
    }}

    static void *asHandle(std::nullptr_t)
    {{
        return nullptr;
    }}

    template<typename TValue>
    static QJSValue toScriptValue(QJSEngine *engine, const TValue &value,
        const QObject *context = nullptr)
    {{
        if (!engine)
            return {{ }};
        if constexpr (std::is_base_of_v<QDotNetRef, TValue>) {{
            QVariant variant = toVariant(QDotNetObject(value), context);
            if (variant.metaType().flags() & QMetaType::PointerToQObject)
                return engine->newQObject(variant.value<QObject *>());
            return engine->toScriptValue(variant);
        }}
        return engine->toScriptValue(QVariant::fromValue(value));
    }}

    template<typename TValue>
    static TValue fromScriptResult(const QJSValue &value)
    {{
        if constexpr (std::is_base_of_v<QDotNetRef, TValue>) {{
            QDotNetObject obj = value.isQObject()
                ? fromVariant(QVariant::fromValue(value.toQObject()))
                : fromVariant(value.toVariant());
            if constexpr (std::is_same_v<TValue, QDotNetObject>)
                return obj;
            return obj.cast<TValue>();
        }}
        return value.toVariant().template value<TValue>();
    }}

    template<typename TResult, typename... TArg>
    class ScriptDelegateContext final : public QDotNetCallback<TResult, TArg...>
    {{
    public:
        ScriptDelegateContext(const QJSValue &function, QQmlEngine *engine, QObject *context)
            : QDotNetCallback<TResult, TArg...>(
                [](void *data, TArg... arg) -> TResult
                {{
                    return reinterpret_cast<ScriptDelegateContext *>(data)->invoke(arg...);
                }}),
              function(function),
              engine(engine),
              context(context)
        {{}}

        static void QDOTNETFUNCTION_CALLTYPE deleteSelf(void *data)
        {{
            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            // Engine already gone: QJSValue is no longer referenced, safe to delete
            // from any thread.
            if (!self->engine) {{
                delete self;
                return;
            }}
            // QJSValue has thread affinity; destroy on the engine thread.
            if (QThread::currentThread() == self->engine->thread()) {{
                delete self;
                return;
            }}
            // GC finalizer runs off the engine thread; post the deletion so the
            // QJSValue destructor runs on the correct thread.
            QMetaObject::invokeMethod(self->engine, [self] {{ delete self; }},
                Qt::QueuedConnection);
        }}

    private:
        TResult invoke(TArg... arg)
        {{
            if (!engine || !function.isCallable())
                return QtDotNet::null<TResult>();
            if (QThread::currentThread() == engine->thread())
                return invokeNow(arg...);

            TResult result = QtDotNet::null<TResult>();
            // NOTE: BlockingQueuedConnection will deadlock if the calling thread also
            // processes Qt events (e.g. is the main/UI thread). Delegates must only be
            // invoked from non-event-loop threads or a dedicated worker thread.
            QMetaObject::invokeMethod(engine,
                [this, &result, arg...]() mutable
                {{
                    result = invokeNow(arg...);
                }},
                Qt::BlockingQueuedConnection);
            return result;
        }}

        TResult invokeNow(TArg... arg)
        {{
            QJSValueList args
            {{
                Convert::toScriptValue(engine, arg, context)...
            }};
            return Convert::fromScriptResult<TResult>(function.call(args));
        }}

        QJSValue function;
        QPointer<QQmlEngine> engine;
        QPointer<QObject> context;
    }};

    template<typename... TArg>
    class ScriptDelegateContext<void, TArg...> final : public QDotNetCallback<void, TArg...>
    {{
    public:
        ScriptDelegateContext(const QJSValue &function, QQmlEngine *engine, QObject *context)
            : QDotNetCallback<void, TArg...>(
                [](void *data, TArg... arg)
                {{
                    reinterpret_cast<ScriptDelegateContext *>(data)->invoke(arg...);
                }}),
              function(function),
              engine(engine),
              context(context)
        {{}}

        static void QDOTNETFUNCTION_CALLTYPE deleteSelf(void *data)
        {{
            auto *self = reinterpret_cast<ScriptDelegateContext *>(data);
            // Engine already gone: QJSValue is no longer referenced, safe to delete
            // from any thread.
            if (!self->engine) {{
                delete self;
                return;
            }}
            // QJSValue has thread affinity; destroy on the engine thread.
            if (QThread::currentThread() == self->engine->thread()) {{
                delete self;
                return;
            }}
            // GC finalizer runs off the engine thread; post the deletion so the
            // QJSValue destructor runs on the correct thread.
            QMetaObject::invokeMethod(self->engine, [self] {{ delete self; }},
                Qt::QueuedConnection);
        }}

    private:
        void invoke(TArg... arg)
        {{
            if (!engine || !function.isCallable())
                return;
            if (QThread::currentThread() == engine->thread()) {{
                invokeNow(arg...);
                return;
            }}

            // NOTE: BlockingQueuedConnection will deadlock if the calling thread also
            // processes Qt events (e.g. is the main/UI thread). Delegates must only be
            // invoked from non-event-loop threads or a dedicated worker thread.
            QMetaObject::invokeMethod(engine,
                [this, arg...]() mutable
                {{
                    invokeNow(arg...);
                }},
                Qt::BlockingQueuedConnection);
        }}

        void invokeNow(TArg... arg)
        {{
            QJSValueList args
            {{
                Convert::toScriptValue(engine, arg, context)...
            }};
            function.call(args);
        }}

        QJSValue function;
        QPointer<QQmlEngine> engine;
        QPointer<QObject> context;
    }};

    template<typename TDelegate, typename TResult, typename... TArg>
    static TDelegate fromScriptDelegate(const QString &delegateTypeName,
        const QJSValue &value, const QObject *context = nullptr)
    {{
        if (!value.isCallable())
            return nullptr;
        auto *qContext = const_cast<QObject *>(context);
        auto *engine = qContext ? qmlEngine(qContext) : nullptr;
        if (!engine) {{
            qWarning() << ""Qt/.NET: cannot create delegate proxy for"" << delegateTypeName
                       << "": no QML engine found for context"";
            return nullptr;
        }}

        auto *callback = new ScriptDelegateContext<TResult, TArg...>(value, engine, qContext);
        return TDelegate(QDotNetAdapter::instance().addDelegateProxy(
            delegateTypeName,
            callback,
            asHandle(&ScriptDelegateContext<TResult, TArg...>::deleteSelf),
            asHandle(ScriptDelegateContext<TResult, TArg...>::delegate()),
            asHandle(ScriptDelegateContext<TResult, TArg...>::cleanUp()),
            callback));
    }}

    template<typename T>
    static T *as(QDotNetObject &obj, bool addRef, const QObject *context = nullptr)
    {{
        static_assert(std::is_base_of_v<QObject, T>, ""as<T> requires a QObject-derived type"");
        if (!obj.type().isAssignableTo<T>())
            return nullptr;
        T *tObj = new T(obj.cast<T>(addRef));
        if (context && QJSEngine::objectOwnership(const_cast<QObject *>(context))
            == QJSEngine::JavaScriptOwnership) {{
            QJSEngine::setObjectOwnership(tObj, QJSEngine::JavaScriptOwnership);
        }}
        return tObj;
    }}

    template<typename T>
    static T *as(QObject *qObj)
    {{
        if (!qObj)
            return nullptr;
        auto dnObj = const_cast<QDotNetObject *>(asDotNetObject(qObj));
        if (!dnObj || !dnObj->isValid())
            return nullptr;
        return as<T>(*dnObj, true, qObj);
    }}

    template<typename T>
    static T *moveToHeap(T &obj, const QObject *context = nullptr)
    {{
        static_assert(std::is_base_of_v<QObject, T> && std::is_base_of_v<QDotNetObject, T>,
            ""moveToHeap<T> requires a QObject- and QDotNetObject-derived type"");
        auto *tObj = new T(std::move(obj));
        if (context && QJSEngine::objectOwnership(const_cast<QObject *>(context))
            == QJSEngine::JavaScriptOwnership) {{
            QJSEngine::setObjectOwnership(tObj, QJSEngine::JavaScriptOwnership);
        }}
        return tObj;
    }}

    template<typename T>
    struct Object
    {{
        static_assert(std::is_base_of_v<QObject, T> && std::is_base_of_v<QDotNetObject, T>,
            ""Object<T> requires a QObject- and QDotNetObject-derived type"");

        static T* null() {{ return nullptr; }}

        static bool isValid(T *obj) {{ return obj && obj->isValid(); }}

        template<typename V>
        static T* toValue(T *obj)
        {{
            static_assert(std::is_same_v<V, T *>, ""Value must be pointer to object type."");
            return obj;
        }}

        template<typename V>
        static T* fromValue(T *obj)
        {{
            static_assert(std::is_same_v<V, T>, ""Value must be of the object type."");
            return obj;
        }}
    }};
}};

// Explicit specialization must be at namespace scope (GCC/Clang are strict here).
template<>
struct Convert::Object<QDotNetObject>
{{
    static QVariant null() {{ return QVariant(); }}

    static bool isValid(const QVariant &obj)
    {{
        if (!obj.isValid())
            return false;
        if (obj.metaType().flags() & QMetaType::PointerToQObject) {{
            if (const QDotNetObject *dnObj = Convert::asDotNetObject(obj.value<QObject *>()))
                return dnObj->isValid();
            else
                return false;
        }}
        return true;
    }}

    template<typename V>
    static V toValue(const QVariant &obj) {{ return obj.value<V>(); }}

    template<typename V>
    static QVariant fromValue(V value) {{ return QVariant::fromValue(value); }}
}};
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var convertCpp = new FilePlaceholder(
                ConvertSource, Root, $"{Root.MFn(Dir)}{convertCppPath}");
            convertCpp += $@"
#include <convert.h>
#include <object_dispatch.h>

QDotNetType &Convert::type()
{{
    static QDotNetType convertType;
    if (!convertType.isValid())
        convertType = QDotNetType::typeOf(Convert::TypeName);
    return convertType;
}}

{string.Join(@"

", funcs.Select(func => $@"{Wrap}
{(!func.ReturnType.IsObject() ? func.ReturnType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
Convert::{func.MFn(Name)}({string.Join(", ", func.GetParameters().Select(arg => $@"{Wrap}
    {(!arg.ParameterType.IsObject() ? arg.ParameterType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
    {arg.MFn(Name | Src)}"))})
{{
    static QDotNetFunction<{Wrap}
        {(!func.ReturnType.IsObject() ? func.ReturnType.MFn(Ns | Name) : "QDotNetObject")}{Wrap}
        {func.GetParameters() switch
            {
                { Length: > 0 } args => ", " + string.Join(", ", args.Select(arg =>
                    !arg.ParameterType.IsObject() ? arg.ParameterType.MFn(Ns | Name)
                        : "QDotNetObject")),
                _ => string.Empty
            }}> fn;
    if (!fn.isValid())
        type().staticMethod(""{func.MFn(Src)}"", fn);
    return fn({string.Join(", ", func.GetParameters().Select(arg => arg.MFn(Name | Src)))});
}}"))}

QDotNetArray<QDotNetObject> Convert::toArray(QDotNetObject obj)
{{
    static QDotNetFunction<QDotNetArray<QDotNetObject>, QDotNetObject> fn;
    if (!fn.isValid())
        type().staticMethod(""ToArray"", fn);
    return fn(obj);
}}

QDotNetObject Convert::fromVariant(const QVariant &value)
{{
    switch (value.typeId()) {{
    case QMetaType::Bool: return fromBoolean(value.toBool());
    case QMetaType::Char:
    case QMetaType::SChar: return fromSByte((qint8)value.toInt());
    case QMetaType::UChar: return fromByte((quint8)value.toInt());
    case QMetaType::Short: return fromInt16((qint16)value.toInt());
    case QMetaType::UShort: return fromUInt16((quint16)value.toInt());
    case QMetaType::Int:
    case QMetaType::Long: return fromInt32(value.toInt());
    case QMetaType::UInt:
    case QMetaType::ULong: return fromUInt32(value.toUInt());
    case QMetaType::LongLong: return fromInt64(value.toLongLong());
    case QMetaType::ULongLong: return fromUInt64(value.toULongLong());
    case QMetaType::Float: return fromSingle(value.toFloat());
    case QMetaType::Double: return fromDouble(value.toDouble());
    case QMetaType::QChar: return fromChar(value.toChar());
    case QMetaType::QString: return fromString(value.toString());
    case QMetaType::QDate:
    case QMetaType::QDateTime: return fromDateTime(value.toDateTime());
    case QMetaType::QUrl: return fromUri(value.toUrl());
    case QMetaType::QPersistentModelIndex:
    case QMetaType::QModelIndex: return fromModelIndex(value.toModelIndex());
    default:
        if (value.metaType().flags() & QMetaType::PointerToQObject) {{
            auto *dnObj = asDotNetObject(value.value<QObject *>());
            if (dnObj && dnObj->isValid())
                return QDotNetObject(*dnObj);
        }}
    }}
    return nullptr;
}}

QVariant Convert::toVariant(QDotNetObject obj, const QObject *context)
{{
    if (isBoolean(obj))
        return QVariant::fromValue(toBoolean(obj));
    if (isSByte(obj))
        return QVariant::fromValue(toSByte(obj));
    if (isByte(obj))
        return QVariant::fromValue(toByte(obj));
    if (isInt16(obj))
        return QVariant::fromValue(toInt16(obj));
    if (isUInt16(obj))
        return QVariant::fromValue(toUInt16(obj));
    if (isInt32(obj))
        return QVariant::fromValue(toInt32(obj));
    if (isUInt32(obj))
        return QVariant::fromValue(toUInt32(obj));
    if (isInt64(obj))
        return QVariant::fromValue(toInt64(obj));
    if (isUInt64(obj))
        return QVariant::fromValue(toUInt64(obj));
    if (isSingle(obj))
        return QVariant::fromValue(toSingle(obj));
    if (isDouble(obj))
        return QVariant::fromValue(toDouble(obj));
    if (isDateTime(obj))
        return QVariant::fromValue(toDateTime(obj));
    if (isChar(obj))
        return QVariant::fromValue(toChar(obj));
    if (isString(obj))
        return QVariant::fromValue(toString(obj));
    if (isUri(obj))
        return QVariant::fromValue(toUri(obj));
    if (isModelIndex(obj))
        return QVariant::fromValue(toModelIndex(obj));
    if (QObject *qObj = QtDotNet::objectDispatch(obj))
        return QVariant::fromValue(qObj);
    return {{ }};
}}

const QDotNetObject *Convert::asDotNetObject(QObject *qObj)
{{
    if (!qObj)
        return nullptr;
    auto *mObj = qObj->metaObject();
    if (!mObj)
        return nullptr;
    int mIdx = mObj->indexOfMethod(""asDotNetObject()"");
    if (mIdx == -1)
        return nullptr;
    const QDotNetObject *dnObj = nullptr;
    if (!mObj->method(mIdx).invoke(qObj, Q_RETURN_ARG(const QDotNetObject *, dnObj)))
        return nullptr;
    return dnObj;
}}

";
            return Ok;
        }
    }
}
