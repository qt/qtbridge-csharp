// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text;

namespace Qt.DotNet
{
    public static class StableTypeNameBuilder
    {
        public static string Build(Type type)
        {
            var builder = new StringBuilder();
            BuildAssemblyQualifiedName(type, builder);
            return builder.ToString();
        }

        private static void BuildAssemblyQualifiedName(Type type, StringBuilder builder)
        {
            BuildTypeSpec(type, builder);
            if (type.IsGenericParameter)
                return;
            builder.Append(", ");
            builder.Append(type.Assembly.GetName().Name);
        }

        private static void BuildTypeSpec(Type type, StringBuilder builder)
        {
            if (type.IsGenericParameter)
                builder.Append(type.Name);
            else if (type.IsGenericType)
                BuildGenericType(type, builder);
            else if (type.IsArray)
                BuildArrayType(type, builder);
            else if (type.IsPointer)
                BuildElementType(type, builder, "*");
            else if (type.IsByRef)
                BuildElementType(type, builder, "&");
            else
                builder.Append(type.FullName ?? type.Name);
        }

        private static void BuildGenericType(Type type, StringBuilder builder)
        {
            var genericDef = type.GetGenericTypeDefinition();
            builder.Append(genericDef.FullName);
            if (type.IsGenericTypeDefinition)
                return;
            builder.Append('[');

            var args = type.GetGenericArguments();
            for (var i = 0; i < args.Length; i++) {
                if (i > 0)
                    builder.Append(',');
                builder.Append('[');
                BuildAssemblyQualifiedName(args[i], builder);
                builder.Append(']');
            }

            builder.Append(']');
        }

        private static void BuildArrayType(Type type, StringBuilder builder)
        {
            if (type.GetElementType() is not { } elementType) {
                builder.Append(type.FullName);
                return;
            }
            BuildTypeSpec(elementType, builder);
            builder.Append('[');
            builder.Append(',', type.GetArrayRank() - 1);
            builder.Append(']');
        }

        private static void BuildElementType(Type type, StringBuilder builder, string suffix)
        {
            if (type.GetElementType() is not { } elementType) {
                builder.Append(type.FullName);
                return;
            }
            BuildTypeSpec(elementType, builder);
            builder.Append(suffix);
        }
    }
}
