/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class TypeName : MetaFunction
    {
        public override int Priority => int.MaxValue;
        protected override string Eval(object src, Enum traits)
        {
            if (src is not Type type || type.IsRootNode())
                return null;

            if (traits.HasTraits(Src)) {
                if (traits.HasTraits(Fqn))
                    return type.AssemblyQualifiedName;
                if (traits.HasTraits(Ns | Name))
                    return type.FullName;
                if (traits.HasTraits(Ns))
                    return type.Namespace;
                return type.Name;
            }

            StringBuilder typeName = new();
            if (traits.HasTraits(Ns)) {
                if (type.Namespace is { Length: > 0 })
                    typeName.Append(type.FormatNamespace("::"));
                else
                    typeName.Append("QtDotNet::Global");
            }
            if (traits.HasTraits(Ns | Name))
                typeName.Append("::");
            if (traits.HasTraits(Name)) {
                typeName.Append(type.FormatName("_"));
                if (traits.HasTraits(Private))
                    typeName.Append("Private");
            }

            if (type.IsArray)
                type = type.GetElementType();

            if (traits.HasTraits(Ns) && !traits.HasTraits(Name))
                return typeName.ToString();

            if (!type.IsGenericType || traits.HasTraits(NoArgs))
                return typeName.ToString();

            var args = type.GetGenericArguments();
            typeName.Append($"_{args.Length}__{string
                .Join("__", args.Select(x => Eval(x, Name)))}");

            return typeName.ToString();
        }
    }
}
