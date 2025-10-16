/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace Qt.Quick
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class QmlFileAttribute : Attribute
    {
        public string Uri { get; set; }
        public string TypeName { get; set; }
        public bool IsRoot { get; set; }
        public string Path { get; set; }
        public string ModulePath => System.IO.Path.GetDirectoryName(Path).Replace('\\', '/');
    }
}
