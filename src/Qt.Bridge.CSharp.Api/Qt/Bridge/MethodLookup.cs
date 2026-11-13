// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge
{
    /// <summary>
    /// Finds the methods of metadata types that cannot be resolved by metadata token.
    /// </summary>
    /// <remarks>
    /// Metadata tokens are only unique within a module. Methods inherited from other modules, and
    /// methods the runtime provides for array types, are therefore looked up by name and
    /// parameter types instead. Used by the native host; not meant to be called directly.
    /// </remarks>
    public static class MethodLookup
    {
        /// <summary>
        /// Returns the public instance method of <paramref name="type"/> with the given name and
        /// parameter types, or <c>null</c> if there is none.
        /// </summary>
        /// <param name="type">Type that declares or inherits the method.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="parameterTypes">
        /// Parameter types as returned by <see cref="Type.ToString"/>, separated by '|'. Unlike
        /// <see cref="Type.FullName"/>, these names contain no assembly versions, so they match
        /// when the app runs on a newer .NET than it was built with.
        /// </param>
        public static MethodInfo Find(Type type, string name, string parameterTypes)
        {
            var expected = string.IsNullOrEmpty(parameterTypes)
                ? []
                : parameterTypes.Split('|');
            return type?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method => method.Name == name
                    && method.GetParameters()
                        .Select(parameter => parameter.ParameterType.ToString())
                        .SequenceEqual(expected));
        }
    }
}
