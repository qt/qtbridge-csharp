// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Traits;

    public class GenerateIndexer : Rule
    {
        internal static bool IsSupported(PropertyInfo prop)
            => !prop.IsStatic() && prop.GetIndexParameters() is { Length: > 0 };

        public override bool Matches(MemberInfo src) => src is PropertyInfo prop
            && IsSupported(prop) && prop.ReflectedType.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not PropertyInfo { ReflectedType: { } type } prop)
                return Error();
            if (type.GetPlaceholder(Placeholders.MetadataMethods) is not { } jsonMethods)
                return Error();

            if (prop.GetMethod is { } getMethod)
                GenerateMethod.Append(jsonMethods, getMethod, prop.MFn(Get));
            if (prop.SetMethod is { } setMethod)
                GenerateMethod.Append(jsonMethods, setMethod, prop.MFn(Set));

            return Ok;
        }
    }
}
