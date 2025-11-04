/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
{
    using MetaFunctions;
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateObjectDispatch : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo _)
        {
            var dispatchHppPath = "hpp/object_dispatch.h";
            var dispatchCppPath = "cpp/object_dispatch.cpp";

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += dispatchHppPath;
            sourceFiles += dispatchCppPath;

            var allTypes = SourceGraph.NodeSet<Type>()
                .Where(type => !type.IsEnum && !type.IsRootNode()
                    && !type.IsAssignableTo(TypeOf<Delegate>()))
                .Distinct()
                .OrderBy(t => t.AssemblyQualifiedName, StringComparer.Ordinal)
                .ToList();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var dispatchHpp = new FilePlaceholder(
                ObjectDispatchHeader, Root, $"{Root.MFn(Dir)}{dispatchHppPath}");
            dispatchHpp += $@"
#pragma once
#include <builtin_types.h>

namespace QtDotNet
{{
    QObject *objectDispatch(QDotNetObject &args);
}}
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var dispatchCpp = new FilePlaceholder(
                ObjectDispatchSource, Root, $"{Root.MFn(Dir)}{dispatchCppPath}");
            dispatchCpp += $@"
#include <object_dispatch.h>
#include <QHash>

{dispatchCpp[new() { Distinct = true, Content = allTypes.Select(t => $@"
#include <{t.MFn(Ns | Dir)}{t.MFn(File)}.h>") }]}

using Factory = QObject *(*)(QDotNetObject &);

static const QHash<QString, Factory>& registry()
{{
    static const QHash<QString, Factory> reg = {{
        {string.Join(@",
        ", allTypes.Select(t => $@"{{
            QStringLiteral(""{t.MFn(Src | Fqn)}""),
            [](QDotNetObject& obj) -> QObject *
            {{
                return QtDotNet::as<{t.MFn(Ns | Name)}>(obj, false);
            }}
        }}"))}
    }};
    return reg;
}}

QObject *QtDotNet::objectDispatch(QDotNetObject &args)
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
