/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace Qt.MetaObject
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class QPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Constant { get; set; }
        public bool Final { get; set; }
        public bool Required { get; set; }
        public int[] Revision { get; set; }
    }
}
