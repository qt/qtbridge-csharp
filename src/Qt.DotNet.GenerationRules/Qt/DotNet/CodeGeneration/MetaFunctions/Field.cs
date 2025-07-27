/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using static Traits;

    public class Field : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            FieldInfo field => traits switch
            {
                Src => field.Name,
                Get | Func => $"fnGet{field.Name}",
                Set | Func => $"fnSet{field.Name}",
                Set => $"set{field.Name}",
                _ => field.Name.FromPascalCase().ToCamelCase()
            },
            _ => null
        };
    }
}
