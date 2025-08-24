/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.Extensions
{
    public static class MemberInfoExtensions
    {
        public static bool IsOverload(this MemberInfo self)
        {
            if (self?.ReflectedType is not { } type)
                return false;
            return self switch
            {
                MethodInfo => type.GetMethods().Count(x => x.Name == self.Name) > 1,
                PropertyInfo => type.GetProperties().Count(x => x.Name == self.Name) > 1,
                _ => false
            };
        }
    }
}
