// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.MetaFunctions
{
    using Extensions;
    using static Traits;

    public class Parameter : CppMetaFunction
    {
        protected override string Eval(object src, Enum traits)
        {
            if (src is not ParameterInfo arg)
                return null;
            string argName = string.IsNullOrWhiteSpace(arg.Name) ? $"arg{arg.Position}" : arg.Name;

            if (traits.HasTraits(Src))
                return argName;

            var argType = arg.ParameterType;
            if (argType.Is<object>() || (!argType.IsValue() && !argType.ExportAsSourceCode()))
                return $"QDotNetConvert::fromVariant({argName})";

            return argName;
        }
    }
}
