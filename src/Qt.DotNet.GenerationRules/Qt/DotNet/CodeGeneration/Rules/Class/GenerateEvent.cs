/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;
using Qt.MetaObject;

namespace Qt.DotNet.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateEvent : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is EventInfo ev;
        public override Result Execute(MemberInfo src)
        {
            if (src is not EventInfo ev)
                return Error();
            var type = src.ReflectedType;

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
            privateIncludes += "#include <event_dispatch.h>";

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
void {type.MFn(Ns | Name | Private)}::{ev.MFn(Handler)}{Wrap}
::handleEvent(const QString &eventName, QDotNetObject &sender, QDotNetObject &args)
{{
    if (!args.type().isAssignableTo<QDotNetEventArgs>())
        return;
{(ev.Name != "PropertyChanged" ? Wrap : $@"
    if (args.type().is<QDotNetPropertyEvent>()) {{
        const auto propertyChangedEvent = args.cast<QDotNetPropertyEvent>(true);
        if (propertyChangedEvent.isValid())
            d->onPropertyChanged(propertyChangedEvent.propertyName());
    }}")}

    auto eventArgs = args.cast<QDotNetEventArgs>();
    if (!eventArgs.isValid())
        return;

    QObject *qEvArgs = QtDotNet::eventDispatch(eventArgs);
    if (!qEvArgs)
        return;

    if (QThread::isMainThread()) {{
        emit d->q->{ev.MFn(Signal)}(qEvArgs);
    }} else {{
        QMetaMethod::fromSignal(&{type.MFn(Ns | Name)}::{ev.MFn(Signal)})
            .invoke(d->q, Qt::BlockingQueuedConnection, qEvArgs);
    }}

    if (qEvArgs && !qEvArgs->parent())
        delete qEvArgs;
}}
{Blank}";
            return Ok;
        }
    }
}
