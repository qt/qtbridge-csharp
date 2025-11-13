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

        public static bool IsDerivedFrom(this Type type, Type baseType)
        {
            if (type.IsAssignableTo(baseType))
                return true;
            if (!baseType.IsGenericTypeDefinition)
                return false;
            var tested = new HashSet<Type>();
            var tests = new Queue<Type>([type]);
            while (tests.TryDequeue(out var test)) {
                if (tested.Contains(test))
                    continue;
                tested.Add(test);
                if (test.BaseType != null && !tested.Contains(test.BaseType))
                    tests.Enqueue(test.BaseType);
                foreach (var iface in test.GetInterfaces().Where(t => !tested.Contains(t)))
                    tests.Enqueue(iface);
                if (!test.IsConstructedGenericType)
                    continue;
                if (test.GenericTypeArguments.Length != baseType.GetGenericArguments().Length)
                    continue;
                if (baseType.MakeGenericType(test.GenericTypeArguments).IsAssignableFrom(test))
                    return true;
            }
            return false;
        }

        public static bool IsNestedIn(this Type type, Type outerType)
        {
            while (type.DeclaringType != null) {
                if (type.DeclaringType == outerType)
                    return true;
                type = type.DeclaringType;
            }
            return false;
        }
    }
}
