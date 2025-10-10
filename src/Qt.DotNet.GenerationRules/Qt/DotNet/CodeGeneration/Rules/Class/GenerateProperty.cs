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

    public class GenerateProperty : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src)
            => src is PropertyInfo prop && !prop.IsStatic()
            && prop.GetIndexParameters() is not { Length: > 0 };
        public override Result Execute(MemberInfo src)
        {
            if (src is not PropertyInfo prop)
                return Error();
            var type = src.ReflectedType;
            var propType = prop.PropertyType;
            var star = propType.IsValue() ? "" : "*";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyDeclarations) is not { } properties)
                return Error();
            properties += $@"
Q_PROPERTY({propType.MFn(Ns | Name)} {star}{prop.MFn()}{(
    !prop.CanRead ? string.Empty : $" READ {prop.MFn(Get)}")}{(
    !prop.CanWrite ? string.Empty : $" WRITE {prop.MFn(Set)}")}{(
    !prop.IsNotifiable() ? string.Empty : $" NOTIFY {prop.MFn(Signal)}")})
{(!prop.CanRead ? Wrap : $"{propType.MFn(Ns | Name)} {star}{prop.MFn(Get)}() const;")}
{(!prop.CanWrite ? Wrap : $"void {prop.MFn(Set)}({propType.MFn(Ns | Name)} {star}value);")}
{(!prop.IsNotifiable() ? Wrap : $@"{Wrap}
{BkSpc}#ifndef Q_MOC_RUN
{BkSpc}#  define PROPERTY_{prop.MFn(Src)}
{BkSpc}#endif
PROPERTY_{prop.MFn(Src)} Q_SIGNAL void {prop.MFn(Signal)}();
{BkSpc}#undef PROPERTY_{prop.MFn(Src)}")}
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } privateIncludes)
                return Error();
            privateIncludes += "#include <QThread>";
            privateIncludes += "#include <QMetaMethod>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
{(!prop.CanRead ? Wrap : $@"{Wrap}
mutable QDotNetFunction<{propType.MFn(Ns | Name)}> {prop.MFn(Get | Func)} = nullptr;")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
mutable QDotNetFunction<void, {propType.MFn(Ns | Name)}> {prop.MFn(Set | Func)} = nullptr;")}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();

            implementation += $@"
{(!prop.CanRead ? Wrap : $@"{Wrap}
{propType.MFn(Ns | Name)} {star}{type.MFn(Ns | Name)}::{prop.MFn(Get)}() const
{{
    auto result = method(""{prop.MFn(Src | Get)}"", d->{prop.MFn(Get | Func)}).invoke(*this);
    {(propType.IsValue() ? "return result;" : $"return d->asQObject(result);")}
}}
{Blank}")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
void {type.MFn(Ns | Name)}::{prop.MFn(Set)}({propType.MFn(Ns | Name)} {star}value)
{{
    method(""{prop.MFn(Src | Set)}"", d->{prop.MFn(Set | Func)}).invoke(*this, {star}value);
}}
{Blank}")}";

            if (!prop.IsNotifiable() || type.GetEvent("PropertyChanged") is not { } ev)
                return Ok;

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(EventSubscribers) is not { } subscribers)
                return Error();
            subscribers += $@"
if (signalTag == ""PROPERTY_{prop.MFn(Src)}"") {{
    if (!d->{ev.MFn(Handler | Var)})
        d->{ev.MFn(Handler | Var)} = new {type.MFn(Name | Private)}::{ev.MFn(Handler)}(this, d);
    return;
}}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyNotifiers) is not { } notifiers)
                return Error();
            notifiers += $@"
if (propertyName == ""{prop.MFn(Src)}"") {{
    QMetaMethod::fromSignal(&{type.MFn(Ns | Name)}::{prop.MFn(Signal)})
        .invoke(q, Qt::DirectConnection);
    return;
}}";

            return Ok;
        }
    }
}
