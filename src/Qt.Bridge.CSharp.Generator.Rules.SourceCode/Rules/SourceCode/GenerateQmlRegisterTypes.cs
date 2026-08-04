// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
{
    using MetaFunctions;
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateQmlRegisterTypes : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;

        public override Result Execute(MemberInfo _)
        {
            var qmlRegTypesHppPath = "hpp/qml_register_types.h";

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += qmlRegTypesHppPath;

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var qmlRegTypesHpp = new FilePlaceholder(
                QmlRegisterTypesHeader, Root, $"{Root.MFn(Dir)}{qmlRegTypesHppPath}");
            qmlRegTypesHpp += $@"
#pragma once
#include <functional>
";
            if (Root.Assembly.QmlRootModule() is { Length: > 0 } rootModule) {
                qmlRegTypesHpp += $@"
void qml_register_types_{rootModule}();
std::function<void()> qml_register_types = qml_register_types_{rootModule};
";
            } else {
                qmlRegTypesHpp += $@"
std::function<void()> qml_register_types = nullptr;
";
            }
            return Ok;
        }
    }
}
