// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using Utils.Collections.Concurrent;
    using static Placeholders;
    using static Traits;

    public class GenerateMethod : Rule
    {
        internal static ConcurrentSet<MethodInfo> AmbiguousMethods { get; } = new();

        public override void Reset() => AmbiguousMethods.Clear();

        internal static bool IsSupported(MethodInfo method)
            => !method.IsStatic && !method.ReflectedType.IsStaticClass();

        public override bool Matches(MemberInfo src) => src is MethodInfo method
            && IsSupported(method) && method.ReflectedType.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not MethodInfo func)
                return Error();

            if (func.MFn(Name) == "getType")
                return Warning($"Reserved name 'getType': {func.DeclaringType.FullName}.{func}; skipped");

            if (AmbiguousMethods.Contains(func))
                return Warning($"Ambiguous: {func.DeclaringType.FullName}.{func}; skipped");

            var paramTypes = func.GetParameters()
                .Select(p => p.ParameterType)
                .Append(func.ReturnType);
            if (paramTypes.Any(t => !t.IsMetadataCompatible()))
                return Warning($"Incompatible: {func.DeclaringType.FullName}.{func}; skipped");

            if (func.ReflectedType.GetPlaceholder(MetadataMethods) is not { } jsonFuncs)
                return Error();

            Append(jsonFuncs, func);
            return Ok;
        }

        internal static void Append(Placeholder jsonFuncs, MethodInfo func, string qtName = null)
        {

            // Metadata tokens are only unique within a module, so only methods of the type's own
            // module are resolved by token. Methods inherited from other modules, and methods
            // the runtime provides for array types, have no usable token; they are looked up by
            // name and parameter types.
            var byToken = func.ReflectedType != null
                && func.Module == func.ReflectedType.Module
                && (func.MetadataToken & 0x00FFFFFF) != 0;
            var dotNetParameterTypes = string.Join(", ", func.GetParameters()
                .Select(p => $@"""{p.ParameterType}"""));

            var returnType = func.ReturnType.MetadataCompatibleType();
            var argTypes = (func.GetParameters() ?? [])
                .Select(p => p.ParameterType.MetadataCompatibleType())
                .ToArray();

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
                    byToken
                        ? $@"""metadataToken"": {func.MetadataToken}"
                        : $@"""parameterTypes"": [{dotNetParameterTypes}]"
                ]
            }]}
}},
""qt"": {{
    {jsonFunc[new(QtInfo, func)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""name"": ""{(qtName is { Length: > 0 } ? qtName : func.MFn(Name))}""",
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
