/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
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

struct Convert
{{
    static inline const QString TypeName = QStringLiteral(""{type.MFn(Src | Fqn)}"");
    static QDotNetType &type();
    {string.Join(@"
    ", funcs.Select(func => $@"{Wrap}
    static {(func.ReturnType.IsBuiltIn() ? func.ReturnType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
{func.MFn(Name)}({string.Join(", ", func.GetParameters().Select(arg => $@"{Wrap}
    {(arg.ParameterType.IsBuiltIn() ? arg.ParameterType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
    {arg.MFn(Name)}"))});"))}
    static QDotNetArray<QDotNetObject> toArray(QDotNetObject obj);
    static QVariant toVariant(QDotNetObject obj);
}};
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var convertCpp = new FilePlaceholder(
                ConvertSource, Root, $"{Root.MFn(Dir)}{convertCppPath}");
            convertCpp += $@"
#include <convert.h>
#include <system/object.h>

QDotNetType &Convert::type()
{{
    static QDotNetType convertType;
    if (!convertType.isValid())
        convertType = QDotNetType::typeOf(Convert::TypeName);
    return convertType;
}}

{string.Join(@"

", funcs.Select(func => $@"{Wrap}
{(func.ReturnType.IsBuiltIn() ? func.ReturnType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
Convert::{func.MFn(Name)}({string.Join(", ", func.GetParameters().Select(arg => $@"{Wrap}
    {(arg.ParameterType.IsBuiltIn() ? arg.ParameterType.MFn(Ns | Name) : "QDotNetObject")} {Wrap}
    {arg.MFn(Name)}"))})
{{
    static QDotNetFunction<{Wrap}
        {(func.ReturnType.IsBuiltIn() ? func.ReturnType.MFn(Ns | Name) : "QDotNetObject")}{Wrap}
        {func.GetParameters() switch
            {
                { Length: > 0 } args => ", " + string.Join(", ", args.Select(arg =>
                    arg.ParameterType.IsBuiltIn() ? arg.ParameterType.MFn(Ns | Name)
                        : "QDotNetObject")),
                _ => string.Empty
            }}> fn;
    if (!fn.isValid())
        type().staticMethod(""{func.MFn(Src)}"", fn);
    return fn({string.Join(", ", func.GetParameters().Select(arg => arg.MFn(Name)))});
}}"))}

QDotNetArray<QDotNetObject> Convert::toArray(QDotNetObject obj)
{{
    static QDotNetFunction<QDotNetArray<QDotNetObject>, QDotNetObject> fn;
    if (!fn.isValid())
        type().staticMethod(""ToArray"", fn);
    return fn(obj);
}}

QVariant Convert::toVariant(QDotNetObject obj)
{{
    if (isBoolean(obj))
        return QVariant(toBoolean(obj));
    if (isSByte(obj))
        return QVariant(toSByte(obj));
    if (isByte(obj))
        return QVariant(toByte(obj));
    if (isInt16(obj))
        return QVariant(toInt16(obj));
    if (isUInt16(obj))
        return QVariant(toUInt16(obj));
    if (isInt32(obj))
        return QVariant(toInt32(obj));
    if (isUInt32(obj))
        return QVariant(toUInt32(obj));
    if (isInt64(obj))
        return QVariant(toInt64(obj));
    if (isUInt64(obj))
        return QVariant(toUInt64(obj));
    if (isSingle(obj))
        return QVariant(toSingle(obj));
    if (isDouble(obj))
        return QVariant(toDouble(obj));
    if (isString(obj))
        return QVariant(toString(obj));
    return QVariant::fromValue<QObject *>(QtDotNet::as<System::Object>(obj));
}}
";
            return Ok;
        }
    }
}
