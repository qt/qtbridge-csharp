// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.IO.Hashing;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Extensions
{
    public static class MemberInfoExtensions
    {
        public static string UniqueId(this MemberInfo m)
        {
            Debug.Assert(m?.ReflectedType?.FullName is { Length: > 0 });
            var data = Encoding.UTF8.GetBytes($"[{m.ReflectedType.FullName}]::{m}");
            return BitConverter.ToString(Crc32.Hash(data)).Replace("-", "");
        }
    }
}
