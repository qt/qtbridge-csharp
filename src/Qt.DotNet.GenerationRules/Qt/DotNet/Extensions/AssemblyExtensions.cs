/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;
using Qt.Quick;

namespace Qt.DotNet.Extensions
{
    using CodeGeneration;

    public static class AssemblyExtensions
    {
        public static string[] QmlFiles(this Assembly self)
        {
            return self.QtAttributeData()
                .Where(x => x.AttributeType.Is<QmlFileAttribute>())
                .SelectMany(x => x.NamedArguments
                    .Where(y => y.MemberName == "Path" && y.TypedValue.ArgumentType.Is<string>())
                    .Select(y => y.TypedValue.Value as string))
                .ToArray();
        }
    }
}
