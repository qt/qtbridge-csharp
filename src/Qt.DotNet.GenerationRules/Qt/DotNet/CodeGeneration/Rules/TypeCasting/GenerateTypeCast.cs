/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules.TypeCasting
{
    using static Placeholders;
    using static Traits;

    public class GenerateTypeCast : Class.GenerateClass
    {
        public override bool Matches(MemberInfo src)
            => base.Matches(src) && src != TypeOf<TypeCast>();
        public override IEnumerable<MemberInfo> DependsOn => [TypeOf<TypeCast>()];
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            var typeCast = TypeOf<TypeCast>();

            // TO-DO: Generate unique names for cast functions

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (typeCast.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
Q_INVOKABLE {type.MFn(Ns | Name)} *as{type.MFn(Name)}(QObject *obj);
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (typeCast.GetPlaceholder(Includes) is not { } includes)
                return Error();
            includes += $"#include <{type.MFn(Ns | Dir)}{type.MFn(File)}.h>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (typeCast.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
{type.MFn(Ns | Name)} *{typeCast.MFn(Ns | Name)}::as{type.MFn(Name)}(QObject *qObj)
{{
    return Convert::as<{type.MFn(Ns | Name)}>(qObj);
}}
{Blank}";
            return Ok;
        }
    }
}
