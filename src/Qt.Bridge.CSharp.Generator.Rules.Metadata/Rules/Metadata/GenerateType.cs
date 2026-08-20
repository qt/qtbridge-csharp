// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateType : Rule
    {
        public override bool Matches(MemberInfo src) => !src.IsRootNode()
            && src is Type type && type.ExportAsMetadata();
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            if (Root.GetPlaceholder(MetadataTypes) is not { } jsonTypes)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            Placeholder jsonType = null;
            jsonTypes += $@"
{{
    {jsonTypes[jsonType = new(MetadataType, type) { Sorted = false, Separator = "," }]}
}}
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            Placeholder qtInfo = null;
            jsonType += $@"
""dotNet"": {{
    {jsonType[new(DotNetInfo, type)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""name"": ""{type.MFn(Src | Ns | Name)}""",
                    $@"""assemblyQualifiedName"": ""{type.MFn(Src | Fqn)}""",
                    $@"""assemblyFile"": ""{type.Assembly.GetName().Name}""",
                    $@"""assemblyFileHash"": ""{new string('0', 128)}""",
                    $@"""moduleMetadataToken"": {type.Module.MetadataToken}",
                    $@"""metadataToken"": {type.MetadataToken}"
                ]
            }]}
}},
""qt"": {{
    {jsonType[qtInfo = new(QtInfo, type)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""isQmlElement"": {(type.IsQmlElement() ? "true" : "false")}"
                ]
            }]}
}}";
            if (type.IsQmlElement()) {

                qtInfo += $@"
""qml"": {{
    {qtInfo[new(QmlInfo, type)
                {
                    Sorted = false,
                    Separator = ",",
                    Content = [
                        $@"""name"": ""{
                            (type.QmlElementName() is {Length: > 0 } name ? name : type.Name )}""",
                        $@"""module"": ""{Root.Assembly.QmlRootModule()}""",
                        $@"""moduleRevisionMajor"": {new Version(Root.MFn(Version)).Major}",
                        $@"""moduleRevisionMinor"": {new Version(Root.MFn(Version)).Minor}"
                    ]
                }]}
}}";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //

            if (SourceGraph.NodeSet<PropertyInfo>().Any(p => p.ReflectedType == type)) {
                jsonType += $@"
""properties"": [
    {jsonType[new(MetadataProperties, type) { Sorted = true, Separator = "," }]}
]";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //

            if (SourceGraph.NodeSet<EventInfo>().Any(p => p.ReflectedType == type)) {
                jsonType += $@"
""events"": [
    {jsonType[new(MetadataEvents, type) { Sorted = true, Separator = "," }]}
]";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //

            if (SourceGraph.NodeSet<MethodInfo>().Any(p => p.ReflectedType == type)) {
                jsonType += $@"
""methods"": [
    {jsonType[new(MetadataMethods, type) { Sorted = true, Separator = "," }]}
]";
            }

            return Ok;
        }
    }
}
