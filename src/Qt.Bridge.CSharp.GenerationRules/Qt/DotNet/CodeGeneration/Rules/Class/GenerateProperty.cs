// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Rules.Class
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
            var cacheType = propType.IsValue() ? TypeOf<object>() : propType;
            var isDelegateProp = propType.IsAssignableTo(TypeOf<Delegate>());

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (isDelegateProp) {
                if (type.GetPlaceholder(Includes) is not { } includes)
                    return Error();
                includes += "#include <QJSValue>";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyDeclarations) is not { } properties)
                return Error();
            // Delegate-typed properties are exposed to QML as write-only QJSValue setters.
            // TODO: Expose a C#-held delegate back to QML (via typed invoke wrappers or
            // DynamicInvoke-based dispatch).
            var qPropertyType = isDelegateProp ? "QJSValue" : propType.MFn(Ns | Name | Arg);
            var qPropertyRead = (!prop.CanRead || isDelegateProp) ? string.Empty
                : $" READ {prop.MFn(Get)}";
            properties += $@"
Q_PROPERTY({qPropertyType} {prop.MFn()}{qPropertyRead}{(
    !prop.CanWrite ? string.Empty : $" WRITE {prop.MFn(Set)}")}{(
    !prop.IsNotifiable() ? string.Empty : $" NOTIFY {prop.MFn(Signal)}")})
{(!prop.CanRead || isDelegateProp ? Wrap : $"{propType.MFn(Ns | Name | Arg)} {prop.MFn(Get)}() const;")}
{(!prop.CanRead || isDelegateProp ? Wrap : $"{propType.MFn(Ns | Name | Arg)} {prop.MFn(Get)}(bool cached) const;")}
{(!prop.CanWrite ? Wrap : isDelegateProp
    ? $"void {prop.MFn(Set)}(QJSValue value);"
    : $"void {prop.MFn(Set)}({propType.MFn(Ns | Name | Arg)} value);")}
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
{(!prop.CanRead || isDelegateProp ? Wrap : $@"{Wrap}
mutable QDotNetFunction<{propType.MFn(Ns | Name)}> {prop.MFn(Get | Func)} = nullptr;")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
mutable QDotNetFunction<void, {propType.MFn(Ns | Name)}> {prop.MFn(Set | Func)} = nullptr;")}
{(!prop.CanRead || !propType.IsObject() || isDelegateProp ? Wrap
: $@"mutable {cacheType.MFn(Ns | Name | Arg)} cached{prop.MFn(Src)} {Wrap}
    = Convert::Object<{cacheType.MFn(Ns | Name)}>::null();")}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();

            implementation += $@"
{(!prop.CanRead || isDelegateProp ? Wrap : $@"{Wrap}
{propType.MFn(Ns | Name | Arg)} {type.MFn(Ns | Name)}::{prop.MFn(Get)}() const
{{
    return {prop.MFn(Get)}({(prop.IsNotifiable() ? "true" : "false")});
}}

{propType.MFn(Ns | Name | Arg)} {type.MFn(Ns | Name)}::{prop.MFn(Get)}(bool cached) const
{{
    {(!propType.IsObject() ? Wrap : $@"{Wrap}
    if (cached && Convert::Object<{cacheType.MFn(Ns | Name)}>::isValid(d->cached{prop.MFn(Src)}))
        return Convert::Object<{cacheType.MFn(Ns | Name)}>{Wrap}
            ::toValue<{propType.MFn(Ns | Name | Arg)}>(d->cached{prop.MFn(Src)});")}
    auto result = method(""{prop.MFn(Src | Get)}"", d->{prop.MFn(Get | Func)}).invoke(*this);
    return {(!propType.IsObject() ? Wrap : $@"{Wrap}
        Convert::Object<{cacheType.MFn(Ns | Name)}>{Wrap}::{Wrap}
        toValue<{propType.MFn(Ns | Name | Arg)}>(d->cached{prop.MFn(Src)} = ")}{(
            !propType.IsObject() ? "result"
            : propType.Is<object>() ? "Convert::toVariant(result)"
            : "Convert::moveToHeap(result, this)")}{(!propType.IsObject() ? "" : ")")};
}}
{Blank}")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
void {type.MFn(Ns | Name)}::{prop.MFn(Set)}({(isDelegateProp ? "QJSValue" : propType.MFn(Ns | Name | Arg))} value)
{{
    {(!prop.CanRead || !propType.IsObject() || isDelegateProp ? Wrap : $@"{Wrap}
    d->cached{prop.MFn(Src)} = {(propType.IsObject() ? "value" : "QVariant::fromValue(value)")};")}
    method(""{prop.MFn(Src | Set)}"", d->{prop.MFn(Set | Func)}).invoke(*this, {Wrap}
        {(isDelegateProp
            ? $"{propType.MFn(Ns | Name)}::fromScriptValue(value, this)"
            : propType.Is<object>() ? $"Convert::fromVariant(value)" : $"{propType.MFn(Star)}value")});
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
    {(!prop.CanRead || !propType.IsObject() || isDelegateProp ? Wrap : $@"{Wrap}
    cached{prop.MFn(Src)} = Convert::Object<{cacheType.MFn(Ns | Name)}>::null();")}
    QMetaMethod::fromSignal(&{type.MFn(Ns | Name)}::{prop.MFn(Signal)})
        .invoke(q, Qt::DirectConnection);
    return;
}}";

            return Ok;
        }
    }
}
