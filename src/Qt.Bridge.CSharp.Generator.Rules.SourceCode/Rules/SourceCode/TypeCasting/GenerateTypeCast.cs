// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.TypeCasting
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateTypeCast : Class.GenerateClass
    {
        public override bool Matches(MemberInfo src)
            => src is Type type
                && base.Matches(src)
                && src != TypeOf<TypeCast>()
                && !type.IsValueType
                && !type.IsInterface
                && !type.IsAbstract;
        public override IEnumerable<MemberInfo> DependsOn => [TypeOf<TypeCast>()];
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            var typeCast = TypeOf<TypeCast>();
            var castName = $"as_{type.FormatNamespace("_")}_{type.MFn(Name)}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (typeCast.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
Q_INVOKABLE {type.MFn(Ns | Name)} *{castName}(QObject *obj);
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
{type.MFn(Ns | Name)} *{typeCast.MFn(Ns | Name)}::{castName}(QObject *qObj)
{{
    return QDotNetConvert::as<{type.MFn(Ns | Name)}>(qObj);
}}
{Blank}";
            return Ok;
        }
    }
}
