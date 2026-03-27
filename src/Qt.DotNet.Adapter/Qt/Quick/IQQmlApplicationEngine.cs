// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

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
        public interface IQQmlEngine
        {
            void LoadFromModule(string uri, string typeName);
            bool WaitForExit(int timeout = -1);
            void ProcessEvents();
            string Version();
        }
    }
}
