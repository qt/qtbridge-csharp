// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem
{
    /// <summary> Classifies the type of detected Qt Bridge project. </summary>
    public enum QtBridgeProjectType
    {
        /// <summary> The project was not recognized as a Qt Bridge project. </summary>
        Unknown = 0,
        /// <summary> A C# project that uses the Qt Bridge for C# NuGet package. </summary>
        QtBridgeCSharp = 1
    }
}
