// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.DotNet;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class BasicTypes : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            Type type when type.IsBuiltIn() => type switch
            {
                _ when traits.HasTraits(Src) => type.FullName,
                _ when traits.HasTraits(Star) => "",
                _ when type.Is<object>()  => traits.HasTraits(Arg) ? "QVariant" : "QDotNetObject",
                _ when type.Is<sbyte>() => "qint8",
                _ when type.Is<byte>() => "quint8",
                _ when type.Is<short>() => "qint16",
                _ when type.Is<ushort>() => "quint16",
                _ when type.Is<int>() => "qint32",
                _ when type.Is<uint>() => "quint32",
                _ when type.Is<long>() => "qint64",
                _ when type.Is<ulong>() => "quint64",
                _ when type.Is<nint>() => $"void *{Wrap}",
                _ when type.Is<nuint>() => $"void *{Wrap}",
                _ when type.Is<float>() => Unsafe("float"),
                _ when type.Is<double>() => Unsafe("double"),
                _ when type.Is<bool>() => Unsafe("bool"),
                _ when type.Is<char>() => "QChar",
                _ when type.Is<decimal>() => Unsafe("double"),
                _ when type.Is<string>() => "QString",
                _ when type.Is<ModelIndex>() => "QModelIndex",
                _ when type.Is<DateTime>() => "QDateTime",
                _ when type.Is<Uri>() => "QUrl",
                _ when type.Is(typeof(void)) => Unsafe("void"),
                _ => null
            },
            _ => null
        };
    }
}
