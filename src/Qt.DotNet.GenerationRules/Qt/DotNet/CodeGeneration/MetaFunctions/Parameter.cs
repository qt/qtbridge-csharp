/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using static Traits;

    public class Parameter : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            ParameterInfo arg => traits switch
            {
                _ => string.IsNullOrWhiteSpace(arg.Name) ? $"arg{arg.Position}" : arg.Name
            },
            _ => null
        };
    }
}
