/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.Bridge.Extensions
{
    public static class MemberInfoExtensions
    {
        public static bool IsOverrideOf(this MemberInfo i, Assembly a)
        {
            return a.GetTypes().Any(t => i.IsOverrideOf(t));
        }

        public static bool IsOverrideOf<T>(this MemberInfo i)
        {
            return i.IsOverrideOf(typeof(T));
        }

        public static bool IsOverrideOf(this MemberInfo i, Type bt)
        {
            if (i?.ReflectedType?.IsAssignableTo(bt) is not true)
                return false;
            if (i is not MethodInfo m)
                return false;
            if (!m.IsVirtual)
                return false;
            if ((m.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot)
                return false;
            var pars = m.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();
            if (bt?.GetMethod(m.Name, pars) is not { IsVirtual: true } bm)
                return false;
            if ((bm.Attributes & MethodAttributes.VtableLayoutMask) != MethodAttributes.NewSlot)
                return false;
            return true;
        }
    }
}
