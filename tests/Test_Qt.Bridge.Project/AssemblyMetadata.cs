/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Test_Qt.Bridge.Project
{
    public static class AssemblyMetadata
    {
        public static string SelectedVersion { get; private set; }

        [ModuleInitializer]
        internal static void Init()
        {
            SelectedVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(m => m.Key == "SelectedVersion")
                ?.Value;
        }
    }
}
