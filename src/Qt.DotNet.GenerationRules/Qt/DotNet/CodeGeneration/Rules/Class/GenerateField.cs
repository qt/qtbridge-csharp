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

            var type = src.ReflectedType;
            var fieldType = field.FieldType;
            var star = fieldType.IsValue() ? "" : "*";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyDeclarations) is not { } properties)
                return Error();
            properties += $@"
Q_PROPERTY({fieldType.MFn(Ns | Name)} {star}{field.MFn()} {Wrap}
    READ {field.MFn(Get)} {Wrap}
{(field.IsLiteral || field.IsInitOnly ? string.Empty : $@" {Wrap}
    WRITE {field.MFn(Set)}")})
{fieldType.MFn(Ns | Name)} {star}{field.MFn(Get)}() const;
{(field.IsLiteral || field.IsInitOnly ? string.Empty : $@"{Wrap}
void {field.MFn(Set)}({fieldType.MFn(Ns | Name)} {star}value);")}
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
            if (!fieldType.IsValue()) {
                privateMembers += $@"
mutable {fieldType.MFn(Ns | Name)} *cached{field.MFn(Src)} = nullptr;";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
{fieldType.MFn(Ns | Name)} {star}{type.MFn(Ns | Name)}::{field.MFn(Get)}() const
{{
    {(fieldType.IsValue() ? Wrap : $@"{Wrap}
    if (d->cached{field.MFn(Src)} && d->cached{field.MFn(Src)}->isValid())
        return d->cached{field.MFn(Src)};")}
    auto result = fieldGet<{fieldType.MFn(Ns | Name)}>({Wrap}
        ""{field.MFn(Src)}"", d->{field.MFn(Get | Func)})
        .invoke(nullptr, *this);
    {(fieldType.IsValue() ? "return result;" : $@"{Wrap}
    return d->cached{field.MFn(Src)} = d->asQObject(result);")}

}}
{(field.IsLiteral || field.IsInitOnly ? Wrap : $@"
void {type.MFn(Ns | Name)}::{field.MFn(Set)}({fieldType.MFn(Ns | Name)} {star}value)
{{
    {(fieldType.IsValue() ? Wrap : $"d->cached{field.MFn(Src)} = value;")}
    fieldSet<{fieldType.MFn(Ns | Name)}>(""{field.MFn(Src)}"", d->{field.MFn(Set | Func)})
        .invoke(nullptr, *this, {star}value);
}}")}
{Blank}";

            return Ok;
        }
    }
}
