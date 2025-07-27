/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class Property : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            PropertyInfo prop => traits switch
            {
                Src => prop.Name,
                Src | Get => $"get_{prop.Name}",
                Src | Set => $"set_{prop.Name}",
                Get | Func when prop.IsOverload() => $"fnGet{prop.Name}_{prop.MetadataToken:X}",
                Get | Func => $"fnGet{prop.Name}",
                Set | Func when prop.IsOverload() => $"fnSet{prop.Name}_{prop.MetadataToken:X}",
                Set | Func => $"fnSet{prop.Name}",
                Set => $"set{prop.Name}",
                Signal => $"{prop.Name.FromPascalCase().ToCamelCase()}Changed",
                _ => prop.Name.FromPascalCase().ToCamelCase()
            },
            _ => null
        };
    }
}
