/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using static Traits;

    public class Enums : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            string name => traits switch
            {
                Enum => name,
                _ => null
            },
            _ => null
        };
    }
}
