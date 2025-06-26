/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.Extensions
{
    using CodeGeneration;
    using static CodeGeneration.Rule;

    public static class TypeExtensionsForGenerationRules
    {
        public static bool IsBuiltIn(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return type.IsPrimitive || type.Is<decimal>() || type.Is(typeof(void))
                || type.Is<object>() || type.Is<Type>() || type.Is<string>();
        }

        public static string FormatName(this Type type, string separator,
            Func<string, string> formatPart = null, Func<string, string> afterFormat = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            formatPart ??= x => x;
            afterFormat ??= x => x;
            Stack<Type> nestingTypes = new();
            while (type != null) {
                nestingTypes.Push(type);
                type = type.DeclaringType; ;
            }
            return afterFormat(string.Join(separator, nestingTypes
                .Select(x => formatPart(x.Name.Split('`')[0]))));
        }

        public static string FormatNamespace(this Type type, string separator,
            Func<string, string> formatPart = null, Func<string, string> afterFormat = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.Namespace is not { Length: > 0 })
                return string.Empty;
            formatPart ??= x => x;
            afterFormat ??= x => x;
            return afterFormat(string.Join(separator, type.Namespace.Split('.')
                .Select(x => formatPart(x))));
        }

        public static bool Implements(this Type self, Type iface)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (iface == null)
                return false;
            return self.GetInterfaces().Contains(iface);
        }
        public static bool Implements(this Type self, string name) => self.Implements(TypeOf(name));
        public static bool Implements<T>(this Type self) => self.Implements(TypeOf<T>());
    }
}
