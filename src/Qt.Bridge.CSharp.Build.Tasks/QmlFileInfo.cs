// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal sealed class QmlFileInfo(string sourcePath, string modulePath, string typeName)
    {
        public string SourcePath { get; } = sourcePath
            ?? throw new ArgumentNullException(nameof(sourcePath));

        public string ModulePath { get; } = modulePath
            ?? throw new ArgumentNullException(nameof(modulePath));

        public string TypeName { get; } = typeName
            ?? throw new ArgumentNullException(nameof(typeName));
    }
}
