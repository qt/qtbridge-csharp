// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
{
    using Extensions;
    using static Traits;

    public class GenerateQmlDir : GenerateBuildSpec
    {
        protected const string Qml = "qml";
        protected const string QmlDir = Qml + "/";

        public override bool Matches(MemberInfo src) => src.IsRootNode();
        public override Result Execute(MemberInfo _)
        {
            var qmlModules = Root.Assembly.QmlFiles()
                .OrderBy(x => x.TypeName)
                .GroupBy(x => (x.Uri, x.ModulePath));

            foreach (var qmlModule in qmlModules) {
                var uri = qmlModule.Key.Uri;
                var path = qmlModule.Key.ModulePath;
                var qmldir = new FilePlaceholder(uri, Root, $"{Root.MFn(Dir)}{QmlDir}{path}/qmldir")
                {
                    Sorted = false,
                    Content = [$"module {uri}"]
                };
                foreach (var qml in qmlModule)
                    qmldir += $"{qml.TypeName} 1.0 {qml.TypeName}.qml";
            }

            return Ok;
        }
    }
}
