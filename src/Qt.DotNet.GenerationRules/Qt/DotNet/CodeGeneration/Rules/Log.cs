/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
{
    using MetaFunctions;

    public class LogBegin : Rule
    {
        private FilePlaceholder LogFile { get; set; }
        public override int Priority => int.MinValue;
        public override bool Matches(MemberInfo _) => true;
        public override Result Execute(MemberInfo src)
        {
            if (src.IsRootNode())
                LogFile = new("LOG", SourceGraph.Root, "rules_log.txt");
            if (LogFile == null)
                return Error();
            LogFile += src.MFn(Log.Begin);
            return Ok;
        }
    }

    public class LogEnd : Rule
    {
        private Placeholder LogFile { get; set; }
        public override int Priority => int.MaxValue;
        public override bool Matches(MemberInfo _) => true;
        public override Result Execute(MemberInfo src)
        {
            if (src.IsRootNode())
                LogFile = Root.GetPlaceholder("LOG");
            if (LogFile == null)
                return Error();
            LogFile += src.MFn(Log.End);
            return Ok;
        }
    }
}
