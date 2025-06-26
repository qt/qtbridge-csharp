/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class Paths : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            Type type when type.IsRootNode() => traits switch
            {
                Dir or Ns | Dir => "source/",
                _ => null
            },
            Type type => traits switch
            {
                Dir or Ns | Dir when type.Namespace is not { Length: > 0 } => string.Empty,
                Dir or Ns | Dir => type.FormatNamespace("/", x => x.ToLower(), x => x + "/"),
                File => type.FormatName("_", x => x.ToLower()),
                _ => null
            },
            _ => null
        };
    }
}
