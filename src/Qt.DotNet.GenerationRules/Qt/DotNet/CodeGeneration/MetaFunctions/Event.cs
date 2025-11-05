/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using Text;
    using static Traits;

    public class Event : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            EventInfo ev => traits switch
            {
                Handler => $"{ev.Name}EventHandler",
                Handler | Var => $"handler{ev.Name}",
                Signal => ev.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel),
                Src => ev.Name,
                _ => ev.Name
            },
            _ => null
        };
    }
}
