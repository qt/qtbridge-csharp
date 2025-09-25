/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet.Extensions
{
    public static class TypeExtensionsForGenerator
    {
        public static IEnumerable<Type> DelegateSignature(this Type type)
        {
            if (type.GetMethod("Invoke") is not { } invoke)
                return [];
            return invoke.GetParameters()
                .Select(x => x.ParameterType)
                .Prepend(invoke.ReturnType);
        }
    }
}
