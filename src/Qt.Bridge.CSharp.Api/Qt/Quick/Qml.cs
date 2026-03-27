// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using Qt.Bridge.Utils.Text;
using Qt.DotNet;

namespace Qt
{
    namespace Quick
    {
        using static Adapter;

        public static class Qml
        {
            private static readonly Lazy<IQQmlEngine> LazyInstance =
                new(Static.QQmlEngine_Get, isThreadSafe: true);
            internal static IQQmlEngine Instance => LazyInstance.Value;

            public static string RootModule
            {
                get
                {
                    var root = Assembly.GetEntryAssembly()
                        ?.GetCustomAttributes<QmlModuleAttribute>()
                        .FirstOrDefault(a => a.IsRoot)
                        ?.Uri;
                    if (string.IsNullOrEmpty(root))
                        throw new InvalidOperationException("QML root module not found.");
                    return root;
                }
            }

            public static void LoadFromRootModule(string typeName)
            {
                LoadFromModule(RootModule, typeName);
            }

            public static void LoadFromModule(string uri, string typeName)
            {
                Instance.LoadFromModule(uri, typeName.ConvertCase(CaseStyle.Camel, CaseStyle.Pascal));
            }

            public static bool WaitForExit(int timeout = -1)
            {
                return Instance.WaitForExit(timeout);
            }

            public static void ProcessEvents()
            {
                Debug.Assert(IsMainThread, "Qml.ProcessEvents() must be called from main thread.");
                if (Adapter.IsMainThread)
                    Instance.ProcessEvents();
            }
        }
    }

    public static class Globals
    {
        public static Version Version => new(Quick.Qml.Instance.Version());
    }
}
