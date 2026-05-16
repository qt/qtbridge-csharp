// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Test_Qt.Bridge.Project
{
    public static class AssemblyMetadata
    {
        public static string SelectedVersion { get; private set; }

        public static class Build
        {
            public static string ProjectDir { get; private set; }
            public static string Configuration { get; private set; }
            public static string Platform { get; private set; }

            internal static void Init()
            {
                var attribs = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .ToDictionary(a => a.Key, a => a.Value);
                var buildProps = typeof(Build)
                    .GetProperties(BindingFlags.Static | BindingFlags.Public);
                foreach (var buildProp in buildProps) {
                    if (attribs.TryGetValue($"{nameof(Build)}.{buildProp.Name}", out var propValue))
                        buildProp.SetValue(null, propValue);
                }
            }
        }

        [ModuleInitializer]
        internal static void Init()
        {
            SelectedVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(m => m.Key == "SelectedVersion")
                ?.Value;
            Build.Init();
        }
    }
}
