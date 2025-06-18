/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.MetaFunctions
{
    public enum Log { Begin = 100, End }

    public class LogEntry : MetaFunction
    {
        protected override string Eval(object src, Enum traits) => traits switch
        {
            Log log => log switch
            {
                Log.Begin => "BEGIN ",
                Log.End => "END ",
                _ => throw new ArgumentException(nameof(traits))
            }
            + src switch
            {
                Type type => $"TYPE {type.FullName}",
                MemberInfo info => $"{info.MemberType.ToString().ToUpper()} {info.Name}",
                not null => $"??? {src}",
                _ => throw new ArgumentException(nameof(src))
            },
            _ => null
        };
    }
}
