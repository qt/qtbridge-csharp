/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules.Enum
{
    using static Placeholders;
    using static Traits;

    public class GenerateEnumHeader : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is Type { IsEnum: true };
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type { IsEnum: true } type)
                return Error();

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();

            var hppPath = $"{HppDir}{type.MFn(Ns | Dir)}{type.MFn(File)}.h";

            ////////////////////////////////////////////////////////////////////////////////////
            // Header file
            //
            if (type.GetPlaceholder(HppFile) is not { } hppFile) {
                sourceFiles.AddText(hppPath);
                hppFile = new FilePlaceholder(HppFile, type, $"{Root.MFn(Dir)}{hppPath}");
                hppFile += $@"
#pragma once
#include <builtin_types.h>
#include <qdotnetmarshal.h>
#include <QtQml/qqmlregistration.h>

////////////////////////////////////////////////////////////////////////////////////////////////////
// [{type.MFn(Src | Ns | Name)}]

{hppFile[new(PublicDeclarations)]}

// [{type.MFn(Src | Ns | Name)}]
////////////////////////////////////////////////////////////////////////////////////////////////////
";
            }
            //
            // Header file
            ////////////////////////////////////////////////////////////////////////////////////

            return Ok;
        }
    }
}
