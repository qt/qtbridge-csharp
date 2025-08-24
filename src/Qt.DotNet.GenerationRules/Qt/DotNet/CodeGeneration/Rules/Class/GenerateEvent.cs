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

            var signalData = ev.QtAttributeData()
                .Where(a => a.AttributeType.IsAssignableTo(TypeOf<QSignalAttribute>()))
                .Select(a => new
                {
                    Src = a.MFn(Src) switch
                    {
                        { Length: > 0 } name => name,
                        _ => ev.MFn(Src)
                    },
                    Name = a.MFn(Signal) switch
                    {
                        { Length: > 0 } name => name,
                        _ => ev.MFn(Signal)
                    },
                    Types = a.AttributeType.GetGenericArguments() switch
                    {
                        { Length: 0 } => [],
                        { Length: 1 } x => x[0].BaseType.GetGenericArguments() switch
                        {
                            { Length: <= 1 } => [],
                            { } y => y.Skip(1).ToArray(),
                            _ => null
                        },
                        { } x => x.Skip(1).ToArray(),
                        _ => null
                    }
                });
            if (!signalData.Any()) {
                signalData = [ new
                {
                    Src = ev.MFn(Src),
                    Name = ev.MFn(Signal),
                    Types = Array.Empty<Type>()
                }];
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(SignalDeclarations) is not { } signals)
                return Error();
            signals += $@"
{BkSpc}#ifndef Q_MOC_RUN
{BkSpc}#  define EVENT_{ev.MFn(Src)}
{BkSpc}#endif{string.Join("", signalData.Select(signal => $@"
EVENT_{ev.MFn(Src)} Q_SIGNAL void {signal.Name}{Wrap}
    ({string.Join(", ", signal.Types.Select(x => $"{x.MFn(Ns | Name)}"))});"))}
{BkSpc}#undef EVENT_{ev.MFn(Src)}
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } privateIncludes)
                return Error();
            privateIncludes += "#include <QDotNetEventArgs>";
            privateIncludes += "#include <QDotNetSignal>";

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
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
void {type.MFn(Ns | Name | Private)}::{ev.MFn(Handler)}{Wrap}
::handleEvent(const QString &eventName, QDotNetObject &sender, QDotNetObject &args)
{{
    if (!args.type().isAssignableTo<QDotNetEventArgs>())
        return;
{(ev.Name != "PropertyChanged" ? Wrap : $@"
    const auto propertyChangedEvent = args.cast<QDotNetPropertyEvent>();
    if (propertyChangedEvent.isValid())
        d->onPropertyChanged(propertyChangedEvent.propertyName());")}

    auto eventArgs = args.cast<QDotNetEventArgs>();
    if (!eventArgs.isValid())
        return;

    auto eventSignals = QDotNetSignal::fromEvent(eventName, sender);
    for (auto& eventSignal : eventSignals) {{
        if (!QDotNetSignal::convert(eventSignal, sender, eventArgs))
            continue;
        auto signalName = eventSignal.name();
        {string.Join("", signalData.Select(signal => $@"
        if (signalName == ""{signal.Src}""{string
            .Join("", signal.Types.Select((t, i) => $@"
            && eventSignal.is({i}, ""{t.AssemblyQualifiedName}"")"))}) {{
            emit d->q->{signal.Name}({string.Join(", ", signal.Types
                .Select((t, i) => $"eventSignal.arg<{t.MFn(Ns | Name)}>({i})"))});
            continue;
        }}"))}
    }}
}}
{Blank}";
            return Ok;
        }
    }
}
