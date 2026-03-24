// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;

namespace Qt.Quick
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class QmlModuleAttribute : Attribute
    {
        public string Uri { get; set; }
        public bool IsRoot { get; set; }
        public string Path { get; set; }
        public string ModulePath => System.IO.Path.GetDirectoryName(Path).Replace('\\', '/');
    }
}
