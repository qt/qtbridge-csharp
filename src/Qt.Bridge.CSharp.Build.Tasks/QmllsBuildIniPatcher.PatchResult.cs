// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static partial class QmllsBuildIniPatcher
    {
        internal readonly struct PatchResult(bool isReady, bool changed, IniFormat format)
        {
            public bool IsReady { get; } = isReady;

            public bool Changed { get; } = changed;

            public IniFormat Format { get; } = format;
        }
    }
}
