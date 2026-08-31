// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateMethod : Rule
    {
        internal static bool IsSupported(MethodInfo method)
            => !method.IsStatic && !method.ReflectedType.IsStaticClass();

        public override bool Matches(MemberInfo src) => src is MethodInfo method
            && IsSupported(method) && method.ReflectedType.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not MethodInfo func)
                return Error();

            if (func.ReflectedType.GetPlaceholder(MetadataMethods) is not { } jsonFuncs)
                return Error();

            Append(jsonFuncs, func);
            return Ok;
        }

        internal static void Append(Placeholder jsonFuncs, MethodInfo func, string qtName = null)
        {

            var returnType = func.ReturnType switch
            {
                Type t when t.IsBuiltIn() => t,
                _ => TypeOf<object>()
            };

            var argTypes = func.GetParameters()
                ?.Select(p => p.ParameterType switch
                {
                    Type t when t.IsBuiltIn() => t,
                    _ => TypeOf<object>()
                })
                ?.ToArray() ?? [];

            Placeholder jsonFunc = null;
            jsonFuncs += $@"
{{
    {jsonFuncs[jsonFunc = new(MetadataMethod, func) { Sorted = false, Separator = "," }]}
}}
";

            jsonFunc += $@"
""dotNet"": {{
    {jsonFunc[new(DotNetInfo, func)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""name"": ""{func.MFn(Src)}""",
                    $@"""metadataToken"": {func.MetadataToken}"
                ]
            }]}
}},
""qt"": {{
    {jsonFunc[new(QtInfo, func)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    qtName is { Length: > 0 } ? $@"""name"": ""{qtName}""" : string.Empty,
                    $@"""returnType"": ""{returnType.MFn(Ns | Name | Arg)}""",
                    argTypes is not { Length: > 0 } ? string.Empty : $@"
""parameters"": [
{Tab}{string.Join($",\r\n{Tab}", argTypes
    .Select(argType => $@"""{argType.MFn(Ns | Name | Arg)}"""))}
]"
                ]
            }]}
}}";
        }
    }
}
