// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using static System.IO.File;

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
                foreach (var qml in qmlModule) {
                    var isSingleton = false;
                    if (!string.IsNullOrEmpty(qml.FilePath)) {
                        if (!Exists(qml.FilePath))
                            return Error($"QML file not found: \"{qml.FilePath}\"");
                        try {
                            var qmlText = ReadAllText(qml.FilePath);
                            if (qmlText.Contains("Singleton", StringComparison.OrdinalIgnoreCase)) {
                                var qmlDom = QmlDom.Exec(qml.FilePath, "objects", "imports");
                                isSingleton = qmlDom.IsSingleton;
                            }
                        } catch (Exception e)
                            when (e is IOException or InvalidDataException
                                or UnauthorizedAccessException or InvalidOperationException
                                or ArgumentException or TimeoutException
                                or System.ComponentModel.Win32Exception) {
                            var filePath = qml.FilePath;
                            return Error($"Failed to inspect QML file '{filePath}': {e.Message}");
                        }
                    }
                    qmldir +=
                        $"{(isSingleton ? "singleton " : "")}{qml.TypeName} 1.0 {qml.TypeName}.qml";
                }
            }

            return Ok;
        }
    }
}
