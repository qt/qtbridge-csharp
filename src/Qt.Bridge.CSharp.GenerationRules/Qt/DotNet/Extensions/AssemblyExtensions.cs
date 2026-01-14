// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Quick;

namespace Qt.Bridge.Extensions
{
    using CodeGeneration;

    public static class AssemblyExtensions
    {
        public static IEnumerable<QmlFileAttribute> QmlFiles(this Assembly self)
        {
            return self.QtAttributeData()
                .Where(x => x.AttributeType.Is<QmlFileAttribute>())
                .Select(x => new QmlFileAttribute()
                {
                    Uri = x.Property<string>(nameof(QmlFileAttribute.Uri)),
                    TypeName = x.Property<string>(nameof(QmlFileAttribute.TypeName)),
                    IsRoot = x.Property<bool>(nameof(QmlFileAttribute.IsRoot)),
                    Path = x.Property<string>(nameof(QmlFileAttribute.Path))
                });
        }
    }
}
