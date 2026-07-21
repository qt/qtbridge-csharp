// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.ComponentModel;
using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateEvent : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is EventInfo ev
            && ev.AddMethod?.IsStatic == false;
        public override Result Execute(MemberInfo src)
        {
            if (src is not EventInfo ev)
                return Error();
            var type = src.ReflectedType;
            if (ev.EventHandlerType.DelegateSignature()?.ToArray() is not { Length: 3 } evTypes
                || !evTypes[2].IsAssignableTo(TypeOf<EventArgs>())) {
                return Error();
            }
            var argsType = evTypes[2];

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(SignalDeclarations) is not { } signals)
                return Error();
            signals += $@"
{BkSpc}#ifndef Q_MOC_RUN
{BkSpc}#  define EVENT_{ev.MFn(Src)}
{BkSpc}#endif
EVENT_{ev.MFn(Src)} Q_SIGNAL void {ev.MFn(Signal)}(QObject *qEvArgs);
{BkSpc}#undef EVENT_{ev.MFn(Src)}
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } privateIncludes)
                return Error();
            privateIncludes += "#include <QThread>";
            privateIncludes += "#include <QMetaMethod>";
            privateIncludes += "#include <QDotNetEventArgs>";
            privateIncludes += "#include <QDotNetSignal>";
            privateIncludes += "#include <object_dispatch.h>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
Q_DOTNET_EVENT_HANDLER({ev.MFn(Src)}, {type.MFn(Name | Private)}) *{ev.MFn(Handler | Var)} {Wrap}
    = nullptr;";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(EventSubscribers) is not { } subscribers)
                return Error();
            subscribers += $@"
if (signalTag == ""EVENT_{ev.MFn(Src)}"") {{
    if (!d->{ev.MFn(Handler | Var)})
        d->{ev.MFn(Handler | Var)} = new {type.MFn(Name | Private)}::{ev.MFn(Handler)}(this, d);
    return;
}}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(EventUnsubscribers) is not { } unsubscribers)
                return Error();
            unsubscribers += $@"
if ({ev.MFn(Handler | Var)})
    delete {ev.MFn(Handler | Var)};";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(EventHandlers) is not { } eventHandlers)
                return Error();
            eventHandlers += $@"
void {type.MFn(Ns | Name | Private)}::{ev.MFn(Handler)}::handleEvent(
    const QString &eventName, QDotNetObject &sender, QDotNetObject &args)
{{
    if (!args.isValid())
        return;

    // NOTE: Do not change invokeMethod using Qt::AutoConnection
    //   * Event-to-signal mapping and property change notifications need to run on d->q's thread.
    QMetaObject::invokeMethod(d->q, [=]() mutable {{

        QObject *qEvArgs = QtDotNet::objectDispatch(args);
        if (!qEvArgs)
            return;
        {(!argsType.Is<PropertyChangedEventArgs>() ? Wrap : $@"{Wrap}
        auto *qPropEv = qobject_cast<{argsType.MFn(Ns | Name)} *>(qEvArgs);
        if (qPropEv)
            d->onPropertyChanged(qPropEv->propertyName());")}
        QMetaMethod::fromSignal(&{type.MFn(Ns | Name)}::{ev.MFn(Signal)})
            .invoke(d->q, Qt::DirectConnection, qEvArgs);
        delete qEvArgs;

    }});
}}
{Blank}";
            return Ok;
        }
    }
}
