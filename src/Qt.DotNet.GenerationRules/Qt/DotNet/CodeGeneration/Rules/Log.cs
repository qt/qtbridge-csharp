/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
{
    using static Traits;

    public class LogTypes : Rule
    {
        private FilePlaceholder LogFile { get; set; }
        public override int Priority => int.MinValue;
        public override bool Matches(MemberInfo _) => true;
        public override Result Execute(MemberInfo src)
        {
            if (src.IsRootNode())
                LogFile = new("LOG", SourceGraph.Root, "rules_log.txt") { Distinct = true };
            if (LogFile == null)
                return Error();
            LogFile += src switch
            {
                _ when src.IsRootNode() => string.Empty,
                Type type => type.MFn(Ns | Name),
                _ => $"{src.ReflectedType.MFn(Ns | Name)}::{src.ToString()}"
            };
            return Ok;
        }
    }
}
