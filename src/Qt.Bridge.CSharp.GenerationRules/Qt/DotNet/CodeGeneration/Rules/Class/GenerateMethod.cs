/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Rules.Class
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
            if (func.GetParameters() is not { } args)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
Q_INVOKABLE {returnType.MFn(Ns | Name | Arg)} {func.MFn(Name)}({string.Join(", ", args
    .Select(arg => $@"{arg.ParameterType.MFn(Ns | Name | Arg)} {arg.MFn(Name | Src)}"))}) const;
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
{returnType.MFn(Ns | Name | Arg)} {type.MFn(Ns | Name)}::{func.MFn(Name)}({string.Join(", ", args
    .Select(arg => $@"{arg.ParameterType.MFn(Ns | Name | Arg)} {arg.MFn(Name | Src)}"))}) const
{{
    {(returnType.Is(typeof(void)) ? string.Empty :
        "auto result = ")}method(""{func.MFn(Src)}"", d->{func.MFn(Func)}).invoke(*this{args switch
        {
            { Length: > 0 } => ", " + string.Join(", ", args
                .Select(arg => $@"{arg.ParameterType.MFn(Star)}{arg.MFn(Name)}")),
            _ => string.Empty
        }});
{(returnType.Is(typeof(void)) ? Wrap
    : !returnType.IsObject() ? $"{Tab}return result;"
    : returnType.Is<object>() ? $"{Tab}return Convert::toVariant(result);"
    : $"{Tab}return Convert::moveToHeap(result, this);")}
}}
{Blank}";

            return Ok;
        }
    }
}
