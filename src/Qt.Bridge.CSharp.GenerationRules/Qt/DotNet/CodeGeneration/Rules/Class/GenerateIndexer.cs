// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateIndexer : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src)
            => src is PropertyInfo prop && !prop.IsStatic()
            && prop.GetIndexParameters() is { Length: > 0 };
        public override Result Execute(MemberInfo src)
        {
            if (src is not PropertyInfo prop)
                return Error();

            var type = src.ReflectedType;
            var propType = prop.PropertyType;
            if (prop.GetIndexParameters() is not { } args)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
{(!prop.CanRead ? Wrap : $@"{Wrap}
Q_INVOKABLE {propType.MFn(Ns | Name | Arg)} {prop.MFn(Get)}({string.Join(", ", args
    .Select(arg => $@"{arg.ParameterType.MFn(Ns | Name | Arg)} {arg.MFn(Name | Src)}"))}) const;
{Blank}")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
Q_INVOKABLE void {prop.MFn(Set)}({string.Join(", ", args
    .Select(arg => $@"{arg.ParameterType.MFn(Ns | Name | Arg)} {arg.MFn(Name | Src)}"))},
    {propType.MFn(Ns | Name | Arg)} value);
{Blank}")}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
{(prop.CanRead ? $@"mutable QDotNetFunction<{propType.MFn(Ns | Name)}{args switch
            {
                { Length: > 0 } => ", " + string
                    .Join(", ", args.Select(arg => arg.ParameterType.MFn(Ns | Name))),
                _ => string.Empty
            }}> {Wrap}
    {prop.MFn(Get | Func)} = nullptr;" : Wrap)}
{(prop.CanWrite ? $@"mutable QDotNetFunction<void{args switch
            {
                { Length: > 0 } => ", " + string
                    .Join(", ", args.Select(arg => arg.ParameterType.MFn(Ns | Name))),
                _ => string.Empty
            }}, {propType.MFn(Ns | Name)}> {Wrap}
    {prop.MFn(Set | Func)} = nullptr;" : Wrap)}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
{(prop.CanRead ? $@"{Wrap}
{propType.MFn(Ns | Name | Arg)} {type.MFn(Ns | Name)}::{prop.MFn(Get)}({string.Join(", ", args
    .Select(arg => $@"{arg.ParameterType.MFn(Ns | Name | Arg)} {arg.MFn(Name | Src)}"))}) const
{{
    auto result = method(""{prop.MFn(Src | Get)}"", d->{prop.MFn(Get | Func)})
        .invoke(*this{args switch
            {
                { Length: > 0 } => ", " + string.Join(", ", args
                    .Select(arg => $@"{arg.ParameterType.MFn(Star)}{arg.MFn(Name)}")),
                _ => string.Empty
            }});
    {(!propType.IsObject() ? "return result;"
    : propType.Is<object>() ? $"return QDotNetConvert::toVariant(result, this);"
    : $"return QDotNetConvert::moveToHeap(result, this);")}
}}" : string.Empty)}
{(prop.CanWrite ? $@"
void {type.MFn(Ns | Name)}::{prop.MFn(Set)}({string.Join(", ", args
    .Select(arg => $@"{arg.ParameterType.MFn(Ns | Name | Arg)} {arg.MFn(Name | Src)}"))}, {propType.MFn(Ns | Name | Arg)} value)
{{
    method(""{prop.MFn(Src | Set)}"", d->{prop.MFn(Set | Func)})
        .invoke(*this{args switch
    {
        { Length: > 0 } => ", " + string.Join(", ", args
            .Select(arg => $@"{arg.ParameterType.MFn(Star)}{arg.MFn(Name)}")),
        _ => string.Empty
    }}, {(propType.Is<object>() ? $"QDotNetConvert::fromVariant(value)" : $"{propType.MFn(Star)}value")});
}}" : string.Empty)}{Blank}";

            return Ok;
        }
    }
}
