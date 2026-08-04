// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
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
            var dispatchCppPath = "cpp/object_dispatch.cpp";

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += dispatchCppPath;

            var allTypes = SourceGraph.NodeSet<Type>()
                .Where(type => !type.IsEnum && !type.IsRootNode()
                    && !type.IsStaticClass()
                    && !type.IsAssignableTo(TypeOf<Delegate>()))
                .Distinct()
                .OrderBy(t => t.AssemblyQualifiedName, StringComparer.Ordinal)
                .ToList();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var dispatchCpp = new FilePlaceholder(
                ObjectDispatchSource, Root, $"{Root.MFn(Dir)}{dispatchCppPath}");
            dispatchCpp += $@"
#include <object_dispatch.h>
#include <QHash>
#include <QDotNetDynamicObject>

{dispatchCpp[new() { Distinct = true, Content = allTypes.Select(t => $@"
#include <{t.MFn(Ns | Dir)}{t.MFn(File)}.h>") }]}

using Factory = QObject *(*)(QDotNetRef &, const QObject *);

static const QHash<QString, Factory>& registry()
{{
    static const QHash<QString, Factory> reg = {{
        {string.Join(@",
        ", allTypes.Select(t => $@"{{
            QStringLiteral(""{t.MFn(Src | Fqn)}""),
            [](QDotNetRef& obj, const QObject *context) -> QObject *
            {{
                return QDotNetConvert::as<{t.MFn(Ns | Name)}>(obj, false, context);
            }}
        }}"))}
    }};
    return reg;
}}

QObject *QtDotNet::objectDispatch(QDotNetRef &args, const QObject *context)
{{
    const QString key = QDotNetType(args.type()).stableAssemblyQualifiedName();
    if (const auto it = registry().constFind(key); it != registry().cend())
        return (*it)(args, context);
    return QDotNetDynamicObject::dispatch(args, key, context);
}}
";
            return Ok;
        }
    }
}
