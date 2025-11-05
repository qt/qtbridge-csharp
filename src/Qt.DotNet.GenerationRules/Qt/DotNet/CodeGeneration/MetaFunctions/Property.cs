/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using Extensions;
    using Text;
    using static Traits;

    public class Property : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            PropertyInfo prop => traits switch
            {
                Src => prop.Name,
                Src | Get => $"get_{prop.Name}",
                Src | Set => $"set_{prop.Name}",
                Get | Func => $"fnGet{prop.Name}_{prop.UniqueId()}",
                Set | Func => $"fnSet{prop.Name}_{prop.UniqueId()}",
                Set => $"set{prop.Name}",
                Signal => $"{prop.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel)}Changed",
                _ => prop.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel)
            },
            _ => null
        };
    }
}
