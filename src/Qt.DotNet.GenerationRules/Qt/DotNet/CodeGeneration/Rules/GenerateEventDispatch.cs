/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
{
    using MetaFunctions;
    using Qt.DotNet.Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateEventDispatch : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo _)
        {
            var dispatchHppPath = "hpp/event_dispatch.h";
            var dispatchCppPath = "cpp/event_dispatch.cpp";

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += dispatchHppPath;
            sourceFiles += dispatchCppPath;

            var eventTypes = SourceGraph.NodeSet<EventInfo>()
                .Select(e => e.EventHandlerType.DelegateSignature().ToArray())
                .Where(s => s.Length == 3 && s[0] == TypeOf(typeof(void))
                    && s[1] == TypeOf<object>() && s[2].IsAssignableTo(TypeOf<EventArgs>()))
                .Select(s => s[2])
                .Distinct()
                .OrderBy(t => t.AssemblyQualifiedName, StringComparer.Ordinal)
                .ToList();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var dispatchHpp = new FilePlaceholder(
                EventDispatchHeader, Root, $"{Root.MFn(Dir)}{dispatchHppPath}");
            dispatchHpp += $@"
#pragma once
#include <builtin_types.h>

namespace QtDotNet
{{
    QObject *eventDispatch(QDotNetObject &args);
}}
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var dispatchCpp = new FilePlaceholder(
                EventDispatchSource, Root, $"{Root.MFn(Dir)}{dispatchCppPath}");
            dispatchCpp += $@"
#include <event_dispatch.h>
#include <QHash>

{dispatchCpp[new() { Distinct = true, Content = eventTypes.Select(t => $@"
#include <{t.MFn(Ns | Dir)}{t.MFn(File)}.h>").ToList() }]}

using Factory = QObject *(*)(QDotNetObject &);

static const QHash<QString, Factory>& registry()
{{
    static const QHash<QString, Factory> reg = {{
        {string.Join(@",
        ", eventTypes.Select(t => $@"{{
            QStringLiteral(""{t.MFn(Src | Fqn)}""),
            [](QDotNetObject& obj) -> QObject *
            {{
                return QtDotNet::as<{t.MFn(Ns | Name)}>(obj);
            }}
        }}"))}
    }};
    return reg;
}}

QObject *QtDotNet::eventDispatch(QDotNetObject &args)
{{
    const QString key = args.type().assemblyQualifiedName();
    if (const auto it = registry().constFind(key); it != registry().cend())
        return (*it)(args);
    return nullptr;
}}
";
            return Ok;
        }
    }
}
