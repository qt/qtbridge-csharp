// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Test_Qt.Bridge.Project
{
    using static AssemblyMetadata;

    internal static class Packages
    {
        public static (string, string) QtBridge => ("QtGroup.Qt.Bridge.CSharp.win-x64", SelectedVersion);
    }
}
