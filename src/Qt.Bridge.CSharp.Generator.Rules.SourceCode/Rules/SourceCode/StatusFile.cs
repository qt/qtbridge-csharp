// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.DotNet;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
{
    using Extensions;
    using static Placeholders;

    public class StatusFile : Rule
    {
        public override int Priority => int.MinValue;
        public override bool Matches(MemberInfo src) => src.IsRootNode();

        public override Result Execute(MemberInfo __)
        {
            var typesToGenerate = SourceGraph?.NodeSet<Type>()
                ?.Where(t => t.ExportAsSourceCode()
                    && t.Assembly != TypeOf<TypeCast>()?.Assembly
                    && t.Assembly != TypeOf<Adapter>()?.Assembly);
            if (!typesToGenerate.Any())
                return Ok;

            _ = new FilePlaceholder(Status, Root, "source_code_status.txt")
            {
                Sorted = true,
                Content = typesToGenerate.Select(type => type.AssemblyQualifiedName)
            };

            return Ok;
        }

        internal static bool CheckIn(MemberInfo src, string tag)
        {
            if (Root.GetPlaceholder(Status) is not { } status)
                return false;
            status += src switch
            {
                Type type => $"{type.AssemblyQualifiedName} | [{tag}]",
                _ => $"{src.ReflectedType.AssemblyQualifiedName} | {src} | [{tag}]"
            };
            return true;
        }
    }
}
