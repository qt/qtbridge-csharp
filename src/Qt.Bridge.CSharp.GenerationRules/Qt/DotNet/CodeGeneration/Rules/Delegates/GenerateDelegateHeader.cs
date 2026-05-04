// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Delegates
{
    using MetaFunctions;
    using static Placeholders;
    using static Traits;

    public class GenerateDelegateHeader : GenerateBuildSpec
    {
        private readonly object criticalSection = new();

        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src)
            => src is Type type && type.IsAssignableTo(TypeOf<Delegate>());
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();

            var baseType = type switch
            {
                { IsGenericType: true } => type.GetGenericTypeDefinition(),
                _ => type
            };

            var hppPath = $"{HppDir}{baseType.MFn(Ns | Dir)}{baseType.MFn(File)}.h";

            Placeholder includes = null, publicDecl = null;

            lock (criticalSection) {

                ////////////////////////////////////////////////////////////////////////////////////
                // Header file
                //
                if (baseType.GetPlaceholder(HppFile) is not { } hppFile) {
                    sourceFiles.AddText(hppPath);
                    hppFile = new FilePlaceholder(HppFile, baseType, $"{Root.MFn(Dir)}{hppPath}");

                    hppFile += $@"

#pragma once
#include <type_traits>
#include <builtin_types.h>
#include <convert.h>

namespace {baseType.MFn(Ns)}
{{
    {hppFile[new(ForwardDecl) { Distinct = true }]}
}}

{hppFile[includes = new(Includes) { Distinct = true }]}

{hppFile[publicDecl = new(PublicDeclarationsGroup)]}
";
                }
                //
                // Header file
                ////////////////////////////////////////////////////////////////////////////////////

            } // END criticalSection

            if (baseType != type) {
                baseType.GetPlaceholder(ForwardDecl).CreateAlias(type);
                includes = baseType.GetPlaceholder(Includes).CreateAlias(type);
            } else {
                includes ??= type.GetPlaceholder(Includes);
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            // Dependency includes
            //
            foreach (var connectedType in SourceGraph.Connected(type))
                includes += $"#include <{connectedType.MFn(Ns | Dir)}{connectedType.MFn(File)}.h>";

            ////////////////////////////////////////////////////////////////////////////////////////
            // Public declarations
            //
            publicDecl ??= baseType.GetPlaceholder(PublicDeclarationsGroup);
            publicDecl += $@"
////////////////////////////////////////////////////////////////////////////////////////////////////
// [{type.MFn(Src | Ns | Name)}]

{publicDecl[new(PublicDeclarations, type)]}

// [{type.MFn(Src | Ns | Name)}]
////////////////////////////////////////////////////////////////////////////////////////////////////
{Blank}";

            return Ok;
        }
    }
}
