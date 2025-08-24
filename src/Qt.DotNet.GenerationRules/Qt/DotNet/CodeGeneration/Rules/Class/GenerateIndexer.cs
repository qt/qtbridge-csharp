/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.DotNet.CodeGeneration.Rules.Class
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
            var propParams = prop.GetIndexParameters();
            var args = string.Join(", ", propParams
                .Select(arg => $"{arg.ParameterType.MFn(Ns | Name)} {arg.MFn(Name)}"));
            var argNames = string.Join(", ", propParams
                .Select(arg => arg.MFn(Name)));
            var argTypes = string.Join(", ", propParams
                .Select(arg => arg.ParameterType.MFn(Ns | Name)));

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
{(!prop.CanRead ? Wrap : $@"{Wrap}
Q_INVOKABLE {propType.MFn(Ns | Name)} {prop.MFn(Get)}({args});
{Blank}")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
Q_INVOKABLE void {prop.MFn(Set)}({args}, {propType.MFn(Ns | Name)} value);
{Blank}")}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
{(prop.CanRead ? $@"mutable QDotNetFunction<{propType.MFn(Ns | Name)}, {argTypes}> {Wrap}
    {prop.MFn(Get | Func)} = nullptr;" : Wrap)}
{(prop.CanWrite ? $@"mutable QDotNetFunction<void, {argTypes}, {propType.MFn(Ns | Name)}> {Wrap}
    {prop.MFn(Set | Func)} = nullptr;" : Wrap)}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
{(prop.CanRead ? $@"{Wrap}
{propType.MFn(Ns | Name)} {type.MFn(Ns | Name)}::{prop.MFn(Get)}({args})
{{
    return method(""{prop.MFn(Src | Get)}"", d->{prop.MFn(Get | Func)}).invoke(*this, {argNames});
}}" : string.Empty)}

{(prop.CanWrite ? $@"{Wrap}
void {type.MFn(Ns | Name)}::{prop.MFn(Set)}({args}, {propType.MFn(Ns | Name)} value)
{{
    method(""{prop.MFn(Src | Set)}"", d->{prop.MFn(Set | Func)}).invoke(*this, {argNames}, value);
}}" : string.Empty)}
{Blank}";

            return Ok;
        }
    }
}
