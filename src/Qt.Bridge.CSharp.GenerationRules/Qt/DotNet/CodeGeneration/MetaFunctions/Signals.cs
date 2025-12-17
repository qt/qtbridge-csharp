/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.MetaObject;
using System.Reflection;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Text;
    using static Traits;

    public class Signals : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits) => src switch
        {
            CustomAttributeData a when a.AttributeType.IsAssignableTo(TypeOf<QSignalAttribute>())
                => traits switch
                {
                    Src => a.NamedArguments
                        .FirstOrDefault(x => x.MemberName == "Name")
                        .TypedValue.Value as string ?? string.Empty,
                    Signal => Eval(src, Src).ConvertCase(CaseStyle.Pascal, CaseStyle.Camel),
                    _ => null
                },
            _ => null
        };
    }
}
