// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text.RegularExpressions;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Utils.Text;
    using static Traits;

    public class BuildSpec : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            Type type when type.IsRootNode() => type switch
            {
                _ when traits.HasTraits(Target) => Regex
                    .Replace(type.Assembly?.GetName()?.Name ?? "Qt.Bridge.App",
                        @"[\W_]|(?<=[a-z])(?=[A-Z])", ".")
                    .Split('.').ToCase(CaseStyle.Snake),
                _ when traits.HasTraits(Version) => "1.0",
                _ => null
            },
            _ => null
        };
    }
}
