/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;
using Qt.DotNet;
using Qt.DotNet.Text;

namespace Qt
{
    namespace DotNet
    {
        public partial class Adapter
        {
            public partial interface IStatic
            {
                Qt.Quick.IQQmlApplicationEngine QQmlApplicationEngine_Get();
            }
            public static Qt.Quick.IQQmlApplicationEngine QQmlApplicationEngine() =>
                Static.QQmlApplicationEngine_Get();
        }
    }

    namespace Quick
    {
        using static DotNet.Adapter;

        public interface IQQmlApplicationEngine
        {
            void LoadFromModule(string uri, string typeName);
            bool WaitForExit(int timeout = -1);
            void ProcessEvents();
        }

        public static class Qml
        {
            private static IQQmlApplicationEngine _Instance;
            private static IQQmlApplicationEngine Instance
            {
                get
                {
                    _Instance ??= Static.QQmlApplicationEngine_Get();
                    return _Instance;
                }
            }

            public static void LoadFromRootModule(string typeName)
            {
                LoadFromModule("Application", typeName);
            }

            public static void LoadFromModule(string uri, string typeName)
            {
                Instance.LoadFromModule(uri, typeName.ConvertCase(CaseStyle.Camel, CaseStyle.Pascal));
            }

            public static bool WaitForExit(int timeout = -1)
            {
                return Instance.WaitForExit(timeout);
            }

            internal static void ProcessEvents()
            {
                if (Adapter.IsMainThread)
                    Instance.ProcessEvents();
            }
        }
    }
}
