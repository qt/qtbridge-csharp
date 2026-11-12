// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class Paths : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            Type type when type.IsRootNode() => traits switch
            {
                Dir or Ns | Dir => "source/",
                _ => null
            },
            Type type => traits switch
            {
                Dir or Ns | Dir when type.Namespace is not { Length: > 0 } => string.Empty,
                Dir or Ns | Dir => type.FormatNamespace("/", x => x.ToLower(), x => x + "/"),
                // Classes and delegates share files per generic definition. Arrays and enums
                // have files per concrete type and must retain their constructed arguments.
                File => type.IsArray || type.IsEnum
                    ? ConcreteFileName(type) : DefinitionFileName(type),
                _ => null
            },
            _ => null
        };

        private static string DefinitionFileName(Type type)
            => type.FormatName("_", name => name.ToLower(),
                nameOf: part => part.Name.Replace('`', '_'));

        private static string ConcreteFileName(Type type)
        {
            if (type.IsArray) {
                var rank = type.IsSZArray ? string.Empty : $"{type.GetArrayRank()}d";
                return $"array{rank}_{ConcreteFileName(type.GetElementType())}";
            }

            var name = DefinitionFileName(type);
            if (type.IsGenericType) {
                name += "__" + string.Join("__", type.GetGenericArguments().Select(argument =>
                    argument.FormatNamespace("_", part => part.ToLower(), ns => ns + "_")
                    + ConcreteFileName(argument)));
            }
            return name;
        }
    }
}
