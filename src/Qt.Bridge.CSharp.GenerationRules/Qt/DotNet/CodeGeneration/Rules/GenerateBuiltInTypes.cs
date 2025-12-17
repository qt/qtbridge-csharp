/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules
{
    using MetaFunctions;
    using static Placeholders;
    using static Traits;

    public class GenerateBuiltInTypes : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo _)
        {
            var builtInPath = "hpp/builtin_types.h";

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += builtInPath;

            var builtIn = new FilePlaceholder(BuiltInTypes, Root, $"{Root.MFn(Dir)}{builtInPath}");
            builtIn += $@"
#pragma once
#include <QtTypes>
#include <QChar>
#include <QModelIndex>
#include <QString>
#include <QObject>
#include <QDotNetObject>
#include <QDotNetType>
#include <QDotNetArray>
#include <QUrl>
";
            return Ok;
        }
    }
}
