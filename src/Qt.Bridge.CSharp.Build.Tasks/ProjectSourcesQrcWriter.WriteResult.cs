// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static partial class ProjectSourcesQrcWriter
    {
        internal readonly struct WriteResult(string? path, bool changed)
        {
            public string? Path { get; } = path;
            public bool Changed { get; } = changed;
        }
    }
}
