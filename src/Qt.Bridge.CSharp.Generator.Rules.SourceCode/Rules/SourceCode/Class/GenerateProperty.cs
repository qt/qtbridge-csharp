// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.Class
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
            var delegateInvoke = isDelegateProp ? propType.GetMethod("Invoke") : null;
            var delegateReturnType = delegateInvoke?.ReturnType;
            var delegateArgs = delegateInvoke?.GetParameters() ?? [];
            var delegateInvokeName = $"invoke{prop.MFn(Src)}";
            var delegateArgNames = delegateArgs.Select((_, i) => $"arg{i}").ToArray();

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
            // Delegate-typed properties accept QJSValue setters. A readable managed delegate is
            // exposed to QML through a generated typed invoker because Qt 6 has no public native
            // QJSValue-function factory.
            var qPropertyType = isDelegateProp ? "QJSValue" : propType.MFn(Ns | Name | Arg);
            var qPropertyRead = (!prop.CanRead || isDelegateProp) ? string.Empty
                : $" READ {prop.MFn(Get)}";
            properties += $@"
Q_PROPERTY({qPropertyType} {prop.MFn()}{qPropertyRead}{(
    !prop.CanWrite ? string.Empty : $" WRITE {prop.MFn(Set)}")}{(
    !prop.IsNotifiable() ? string.Empty : $" NOTIFY {prop.MFn(Signal)}")})
{(!prop.CanRead || isDelegateProp ? Wrap : $"{propType.MFn(Ns | Name | Arg)} {prop.MFn(Get)}() const;")}
{(!prop.CanRead || isDelegateProp ? Wrap : $"{propType.MFn(Ns | Name | Arg)} {prop.MFn(Get)}(bool cached) const;")}
{(!prop.CanRead || !isDelegateProp ? Wrap : $@"Q_INVOKABLE {delegateReturnType.MFn(Ns | Name | Arg)} {delegateInvokeName}({
    string.Join(", ", delegateArgs.Select((arg, i) =>
        $"{arg.ParameterType.MFn(Ns | Name | Arg)} {delegateArgNames[i]}"))}) const;")}
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
{(!prop.CanRead ? Wrap : $@"{Wrap}
mutable QDotNetFunction<{propType.MFn(Ns | Name)}> {prop.MFn(Get | Func)} = nullptr;")}
{(!prop.CanWrite ? Wrap : $@"{Wrap}
mutable QDotNetFunction<void, {propType.MFn(Ns | Name)}> {prop.MFn(Set | Func)} = nullptr;")}
{(!prop.CanRead || !propType.IsObject() || isDelegateProp ? Wrap
: $@"mutable {cacheType.MFn(Ns | Name | Arg)} cached{prop.MFn(Src)} {Wrap}
    = QDotNetConvert::Object<{cacheType.MFn(Ns | Name)}>::null();")}";

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
    if (cached && QDotNetConvert::Object<{cacheType.MFn(Ns | Name)}>::isValid(d->cached{prop.MFn(Src)}))
        return QDotNetConvert::Object<{cacheType.MFn(Ns | Name)}>{Wrap}
            ::toValue<{propType.MFn(Ns | Name | Arg)}>(d->cached{prop.MFn(Src)});")}
    auto result = method(""{prop.MFn(Src | Get)}"", d->{prop.MFn(Get | Func)}).invoke(*this);
    return {(!propType.IsObject() ? Wrap : $@"{Wrap}
        QDotNetConvert::Object<{cacheType.MFn(Ns | Name)}>{Wrap}::{Wrap}
        toValue<{propType.MFn(Ns | Name | Arg)}>(d->cached{prop.MFn(Src)} = ")}{(
            !propType.IsObject() ? "result"
            : propType.Is<object>() ? "QDotNetConvert::toVariant(result, this)"
            : "QDotNetConvert::moveToHeap(result, this)")}{(!propType.IsObject() ? "" : ")")};
}}
{Blank}")}
{(!prop.CanRead || !isDelegateProp ? Wrap : $@"{Wrap}
{delegateReturnType.MFn(Ns | Name | Arg)} {type.MFn(Ns | Name)}::{delegateInvokeName}({
    string.Join(", ", delegateArgs.Select((arg, i) =>
        $"{arg.ParameterType.MFn(Ns | Name | Arg)} {delegateArgNames[i]}"))}) const
{{
    auto callback = method(""{prop.MFn(Src | Get)}"", d->{prop.MFn(Get | Func)}).invoke(*this);
    if (!callback.isValid()){(delegateReturnType.Is(typeof(void)) ? @"
        return;" : $@"
        return {ReturnNull(delegateReturnType)};")}
    {(delegateReturnType.Is(typeof(void)) ? string.Empty : "auto result = ")}callback.invoke({
        string.Join(", ", delegateArgs.Select((arg, i) => DelegateInvokeArg(arg.ParameterType, delegateArgNames[i])))});
{ReturnResult(delegateReturnType)}
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
            : propType.Is<object>() ? $"QDotNetConvert::fromVariant(value)" : $"{propType.MFn(Star)}value")});
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
    cached{prop.MFn(Src)} = QDotNetConvert::Object<{cacheType.MFn(Ns | Name)}>::null();")}
    QMetaMethod::fromSignal(&{type.MFn(Ns | Name)}::{prop.MFn(Signal)})
        .invoke(q, Qt::DirectConnection);
    return;
}}";

            return Ok;

            static string DelegateInvokeArg(Type argType, string argName)
            {
                if (argType.Is<object>())
                    return $"QDotNetConvert::fromVariant({argName})";
                return $"{argType.MFn(Star)}{argName}";
            }

            static string ReturnNull(Type returnType)
            {
                if (returnType.Is<object>())
                    return "QVariant()";
                if (returnType.IsObject())
                    return "nullptr";
                return $"QtDotNet::null<{returnType.MFn(Ns | Name)}>()";
            }

            static string ReturnResult(Type returnType)
            {
                if (returnType.Is(typeof(void)))
                    return Wrap.ToString();
                if (!returnType.IsObject())
                    return $"{Tab}return result;";
                if (returnType.Is<object>())
                    return $"{Tab}return QDotNetConvert::toVariant(result, this);";
                return $"{Tab}return QDotNetConvert::moveToHeap(result, this);";
            }
        }
    }
}
