/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.DotNet.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateMethod : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is MethodInfo { IsStatic: false };
        public override Result Execute(MemberInfo src)
        {
            if (src is not MethodInfo func)
                return Error();

            var type = src.ReflectedType;
            var returnType = func.ReturnType;
            var star = returnType.IsValue() ? "" : "*";

            if (func.GetParameters() is not { } args)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
Q_INVOKABLE {returnType.MFn(Ns | Name)} {star}{func.MFn(Name)}({string.Join(", ", args
    .Select(arg => $@"{Wrap}
        {arg.ParameterType.MFn(Ns | Name)} {(arg.ParameterType.IsValue() ? "" : "*")}{Wrap}
        {arg.MFn(Name)}"))}) const;
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
mutable QDotNetFunction<{returnType.MFn(Ns | Name)}{args switch
            {
                { Length: > 0 } => ", " + string
                    .Join(", ", args.Select(arg => arg.ParameterType.MFn(Ns | Name))),
                _ => string.Empty
            }}> {func.MFn(Func)} = nullptr;";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
{returnType.MFn(Ns | Name)} {star}{type.MFn(Ns | Name)}::{func.MFn(Name)}({string.Join(", ", args
    .Select(arg => $@"{Wrap}
        {arg.ParameterType.MFn(Ns | Name)} {(arg.ParameterType.IsValue() ? "" : "*")}{Wrap}
        {arg.MFn(Name)}"))}) const
{{
    {(returnType.Is(typeof(void)) ? string.Empty :
        "auto result = ")}method(""{func.MFn(Src)}"", d->{func.MFn(Func)}).invoke(*this{args switch
        {
            { Length: > 0 } => ", " + string.Join(", ", args
                .Select(arg => $@"{(arg.ParameterType.IsValue() ? "" : "*")}{arg.MFn(Name)}")),
            _ => string.Empty
        }});
{(returnType.Is(typeof(void)) ? Wrap
    : returnType.IsValue() ? $"{Tab}return result;"
    : $"{Tab}return new {returnType.MFn(Ns | Name)}(std::move(result));")}
}}
{Blank}";

            return Ok;
        }
    }
}
