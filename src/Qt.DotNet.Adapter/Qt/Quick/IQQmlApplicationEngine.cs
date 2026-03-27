// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using Qt.Bridge.Utils.Text;
using Qt.DotNet;

namespace Qt
{
    namespace DotNet
    {
        public partial class Adapter
        {
            public partial interface IStatic
            {
                Qt.Quick.IQQmlEngine QQmlEngine_Get();
            }
            public static Qt.Quick.IQQmlEngine QQmlEngine() =>
                Static.QQmlEngine_Get();
        }
    }

    namespace Quick
    {
        using static DotNet.Adapter;

        public interface IQQmlEngine
        {
            void LoadFromModule(string uri, string typeName);
            bool WaitForExit(int timeout = -1);
            void ProcessEvents();
            string Version();
        }

        public static class Qml
        {
            private static readonly Lazy<IQQmlEngine> LazyInstance =
                new(Static.QQmlEngine_Get, isThreadSafe: true);
            internal static IQQmlEngine Instance => LazyInstance.Value;

            public static string RootModule
            {
                get
                {
                    if (Assembly.GetEntryAssembly()?.GetType("Qt.Qml.Modules") is not { } modules)
                        throw new InvalidOperationException("QML module meta-data not found.");
                    if (modules.GetField("Root")?.GetValue(null) is not string { Length: > 0 } root)
                        throw new InvalidOperationException("Error accessing QML root module.");
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
        public static Version Version => new(Qt.Quick.Qml.Instance.Version());
    }
}
