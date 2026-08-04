// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Utils.Text;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
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
