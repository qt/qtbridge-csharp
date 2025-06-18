/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.MetaObject;
using System;

namespace Qt
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ExcludeAttribute : Attribute
    {
        public ExcludeAttribute(params Type[] excludedTypes)
        { }
        public ExcludeAttribute(params string[] excludedTypeNames)
        { }
        public bool Inherited { get; set; } = false;
    }

    [AttributeUsage(TypeAttributeTarget | MemberAttributeTarget, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
        private const AttributeTargets TypeAttributeTarget
            = AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Interface
            | AttributeTargets.Enum
            | AttributeTargets.Delegate;
        private const AttributeTargets MemberAttributeTarget
            = AttributeTargets.Constructor
            | AttributeTargets.Method
            | AttributeTargets.Property
            | AttributeTargets.Field
            | AttributeTargets.Event;
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GenerateAttribute : Attribute
    {
        public string MainIncludes { get; set; }
        public string MainBeforeAppExec { get; set; }
    }
}
