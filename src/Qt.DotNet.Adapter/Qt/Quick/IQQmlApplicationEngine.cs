/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt
{
    using DotNet.CodeGeneration;

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
        }

        public static class Qml
        {
            private static IQQmlApplicationEngine Instance
            {
                get
                {
                    while (Static == null)
                        Thread.Sleep(100);
                    instance = Static.QQmlApplicationEngine_Get();
                    return instance;
                }
            }
            private static IQQmlApplicationEngine instance;

            public static void LoadFromModule(string typeName)
            {
                LoadFromModule(Assembly.GetCallingAssembly().GetName().Name, typeName);
            }

            public static void LoadFromModule(Assembly assembly, string typeName)
            {
                LoadFromModule(assembly.GetName().Name, typeName);
            }

            public static void LoadFromModule(string uri, string typeName)
            {
                Instance.LoadFromModule(uri, typeName.FromCamelCase().ToPascalCase());
            }

            public static bool WaitForExit(int timeout = -1)
            {
                return Instance.WaitForExit(timeout);
            }
        }
    }
}
