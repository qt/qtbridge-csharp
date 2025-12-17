/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using static Traits;

    public class AssemblyName : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            Type type when type.IsRootNode() => traits switch
            {
                Src => type.Assembly.GetName().Name,
                _ => null
            },
            _ => null
        };
    }
}
