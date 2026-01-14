// Copyright (C) 2025 The Qt Company Ltd.
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
#include <QJSEngine>

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

    template<>
    struct Object<QDotNetObject>
    {{
        static QVariant null() {{ return QVariant(); }}

        static bool isValid(const QVariant &obj)
        {{
            if (!obj.isValid())
                return false;
            if (obj.metaType().flags() & QMetaType::PointerToQObject) {{
                if (const QDotNetObject *dnObj = asDotNetObject(obj.value<QObject *>()))
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
