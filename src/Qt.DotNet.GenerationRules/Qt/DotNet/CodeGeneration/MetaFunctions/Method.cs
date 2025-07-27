/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class Method : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            MethodInfo func => traits switch
            {
                Func when func.IsOverload() => $"fn{func.Name}_{func.MetadataToken:X}",
                Func => $"fn{func.Name}",
                Src => func.Name,
                _ => func.Name.FromPascalCase().ToCamelCase()
            },
            _ => null
        };
    }
}
