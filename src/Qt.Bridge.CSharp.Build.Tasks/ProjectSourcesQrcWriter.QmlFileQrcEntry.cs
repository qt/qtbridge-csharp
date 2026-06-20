// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static partial class ProjectSourcesQrcWriter
    {
        private sealed class QmlFileQrcEntry(QmlFileInfo info, string modulePath, string sourcePath)
        {
            public QmlFileInfo QmlFileInfo { get; } = info;

            public string ModulePath { get; } = modulePath;

            public string SourcePath { get; } = sourcePath;

            public string Alias { get; } = Path.GetFileName(sourcePath);
        }
    }
}
