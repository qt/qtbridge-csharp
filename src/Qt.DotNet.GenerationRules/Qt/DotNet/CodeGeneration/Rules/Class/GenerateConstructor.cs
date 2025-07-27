/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateConstructor : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is ConstructorInfo;
        public override Result Execute(MemberInfo src)
        {
            if (src is not ConstructorInfo ctor)
                return Error();
            var type = src.DeclaringType;

            if (ctor.GetParameters() is not { } args)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(CtorDeclarations) is not { } ctors)
                return Error();
            ctors += $@"
{type.MFn(Name)}({string.Join(", ", args
    .Select(arg => $"{arg.ParameterType.MFn(Ns | Name)} {arg.MFn(Name)}"))});
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
{type.MFn(Ns | Name)}::{type.MFn(Name)}({string.Join(", ", args
    .Select(arg => $"{arg.ParameterType.MFn(Ns | Name)} {arg.MFn(Name)}"))})
    : d(new {type.MFn(Ns | Name | Private)}(this))
{{
    static auto ctor = constructor<{type.MFn(Name)}{args switch
    {
        { Length: > 0 } => ", " + string
            .Join(", ", args.Select(arg => arg.ParameterType.MFn(Ns | Name))),
        _ => string.Empty
    }}>();
    *this = ctor({string.Join(", ", args.Select(arg => arg.MFn(Name)))});
}}
{Blank}";

            return Ok;
        }
    }
}
