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

    public class GenerateField : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is FieldInfo { IsStatic: false };
        public override Result Execute(MemberInfo src)
        {
            if (src is not FieldInfo field)
                return Error();

            var type = src.DeclaringType;
            var fieldType = field.FieldType;

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyDeclarations) is not { } properties)
                return Error();
            properties += $@"
Q_PROPERTY({fieldType.MFn(Ns | Name)} {field.MFn()} {Wrap}
    READ {field.MFn(Get)}{Wrap}
{(field.IsLiteral || field.IsInitOnly ? string.Empty : $@" {Wrap}
    WRITE {field.MFn(Set)}")})
{fieldType.MFn(Ns | Name)} {field.MFn(Get)}();
{(field.IsLiteral || field.IsInitOnly ? string.Empty : $@"{Wrap}
void {field.MFn(Set)}({fieldType.MFn(Ns | Name)} value);")}
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
mutable QDotNetFunction<{fieldType.MFn(Ns | Name)}, QDotNetRef> {Wrap}
    {field.MFn(Get | Func)} = nullptr;
{(field.IsLiteral || field.IsInitOnly ? string.Empty : $@"{Wrap}
mutable QDotNetFunction<void, QDotNetRef, {fieldType.MFn(Ns | Name)}> {Wrap}
    {field.MFn(Set | Func)} = nullptr;")}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
{fieldType.MFn(Ns | Name)} {type.MFn(Ns | Name)}::{field.MFn(Get)}()
{{
    return fieldGet<{fieldType.MFn(Ns | Name)}>(""{field.MFn(Src)}"", d->{field.MFn(Get | Func)})
        .invoke(nullptr, *this);
}}
{(field.IsLiteral || field.IsInitOnly ? Wrap : $@"
void {type.MFn(Ns | Name)}::{field.MFn(Set)}({fieldType.MFn(Ns | Name)} value)
{{
    fieldSet<{fieldType.MFn(Ns | Name)}>(""{field.MFn(Src)}"", d->{field.MFn(Set | Func)})
        .invoke(nullptr, *this, value);
}}")}
{Blank}";

            return Ok;
        }
    }
}
