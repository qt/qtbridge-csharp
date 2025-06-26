/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Text.RegularExpressions;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using static Traits;

    public class BuildSpec : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            Type type when type.IsRootNode() => type switch
            {
                _ when traits.HasTraits(Target) => Regex
                    .Replace(type.Assembly?.GetName()?.Name ?? "Qt.DotNet.App",
                        @"[\W_]|(?<=[a-z])(?=[A-Z])", ".")
                    .Split('.').ToSnakeCase(),
                _ when traits.HasTraits(Version) => "1.0",
                _ => null
            },
            _ => null
        };
    }
}
