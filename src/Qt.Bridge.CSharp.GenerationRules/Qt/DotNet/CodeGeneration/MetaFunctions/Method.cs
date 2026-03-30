// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Extensions;
    using Utils.Text;
    using static Traits;

    public class Method : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            MethodInfo func => traits switch
            {
                Func => $"fn{func.Name}_{func.UniqueId()}",
                Src => func.Name,
                _ => func.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel)
            },
            _ => null
        };
    }
}
