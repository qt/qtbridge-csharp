// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Utils.Text;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using static Traits;

    public class Field : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            FieldInfo field => traits switch
            {
                Src => field.Name,
                Get | Func => $"fnGet{field.Name}",
                Set | Func => $"fnSet{field.Name}",
                Set => $"set{field.Name}",
                _ => field.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel)
            },
            _ => null
        };
    }
}
