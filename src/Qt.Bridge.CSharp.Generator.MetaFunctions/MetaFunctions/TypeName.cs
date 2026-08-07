// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text;
using Qt.DotNet;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class TypeName : CppMetaFunction
    {
        public override int Priority => int.MaxValue;
        protected override string Eval(object src, Enum traits)
        {
            if (src is not Type type || type.IsRootNode())
                return null;

            if (traits.HasTraits(Src)) {
                if (traits.HasTraits(Fqn))
                    return StableTypeNameBuilder.Build(type);
                if (traits.HasTraits(Ns | Name))
                    return type.FullName;
                if (traits.HasTraits(Ns))
                    return type.Namespace;
                if (traits.HasTraits(File))
                    return type.Assembly.GetName().Name + ".dll";
                return type.Name;
            }

            if (!type.ExportAsSourceCode()) {
                return type switch
                {
                    _ when traits.HasTraits(Star) => "",
                    _ => traits.HasTraits(Arg) ? "QVariant" : "QDotNetObject",
                };
            }

            if (traits.HasTraits(Star) && !type.IsEnum)
                return type.IsValue() ? "" : "*";

            if (traits.HasTraits(Arg))
                return $"{src.MFn((TraitFlags)traits & ~Arg)}{(type.IsEnum ? "" : $" *{Wrap}")}";


            StringBuilder typeName = new();
            if (traits.HasTraits(Ns)) {
                if (type.Namespace is { Length: > 0 })
                    typeName.Append(type.FormatNamespace("::", ns => Sanitize(ns)));
                else
                    typeName.Append("QtDotNet::Global");
            }
            if (traits.HasTraits(Ns | Name))
                typeName.Append("::");
            if (traits.HasTraits(Name)) {
                typeName.Append(type.FormatName("_", n => Sanitize(n)));
                if (traits.HasTraits(Private))
                    typeName.Append("Private");
                if (traits.HasTraits(Init))
                    typeName.Append("Init");
                if (traits.HasTraits(Enum))
                    typeName.Append("Enum");
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
