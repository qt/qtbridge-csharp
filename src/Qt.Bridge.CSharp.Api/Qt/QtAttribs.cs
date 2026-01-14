// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using Qt.MetaObject;

namespace Qt
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoreTypeAttribute : Attribute
    {
        public IgnoreTypeAttribute(params Type[] excludedTypes)
        { }
        public IgnoreTypeAttribute(params string[] excludedTypeNames)
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

    [AttributeUsage(TypeAttributeTarget, AllowMultiple = false)]
    public class IncludeAttribute : Attribute
    {
        private const AttributeTargets TypeAttributeTarget
            = AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Interface
            | AttributeTargets.Enum
            | AttributeTargets.Delegate;
    }

    [AttributeUsage(MemberAttributeTarget, AllowMultiple = false)]
    internal class EnableAttribute : IncludeAttribute
    {
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
        public string Packages { get; set; }
        public string Libraries { get; set; }
    }
}
