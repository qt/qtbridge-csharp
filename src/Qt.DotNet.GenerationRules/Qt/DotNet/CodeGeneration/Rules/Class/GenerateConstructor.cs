/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules.Class
{
    using Extensions;
    using System;
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
            if (type.GetPlaceholder(CtorDeclarations) is not { } ctorDeclarations)
                return Error();
            ctorDeclarations += $@"
{type.MFn(Name)}({string.Join(", ", args
    .Select(arg => $"{arg.ParameterType.MFn(Ns | Name)} {arg.MFn(Name)}"))});
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PublicCtors) is not { } ctors)
                return Error();
            ctors += $@"
{type.MFn(Ns | Name)}::{type.MFn(Name)}({string.Join(", ", args
    .Select(arg => $"{arg.ParameterType.MFn(Ns | Name)} {arg.MFn(Name)}"))}) :
    QDotNetObject(constructor<{type.MFn(Ns | Name)}{args switch
    {
        { Length: > 0 } => ", " + string
            .Join(", ", args.Select(arg => arg.ParameterType.MFn(Ns | Name))),
        _ => string.Empty
    }}>().invoke(nullptr{args switch
    {
        { Length: > 0 } => ", " + string
            .Join(", ", args.Select(arg => arg.MFn(Name))),
        _ => string.Empty
    }})),
    d(new {type.MFn(Ns | Name | Private)}(this)),
    i(new {type.MFn(Ns | Name | Init)}(this, d))
{{
}}
{Blank}";

            return Ok;
        }
    }
}
