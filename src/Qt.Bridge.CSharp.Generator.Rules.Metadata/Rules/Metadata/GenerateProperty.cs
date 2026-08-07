// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.ComponentModel;
using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateProperty : Rule
    {
        public override bool Matches(MemberInfo src) => src is PropertyInfo prop
            && prop.ReflectedType.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not PropertyInfo prop || prop.ReflectedType is not { } type)
                return Error();

            var exportType = prop.PropertyType;
            if (!exportType.IsBuiltIn())
                exportType = TypeOf<object>();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MetadataProperties) is not { } jsonProps)
                return Error();

            Placeholder jsonProp = null;
            jsonProps += $@"
{{
    {jsonProps[jsonProp = new(MetadataProperty, prop) { Sorted = false, Separator = "," }]}
}}
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            jsonProp += $@"
""dotNet"": {{
    {jsonProp[new(DotNetInfo, prop)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""name"": ""{prop.MFn(Src)}""",
                    $@"""hasGet"": {(prop.CanRead ? "true" : "false")}",
                    $@"""hasSet"": {(prop.CanWrite ? "true" : "false")}",
                    $@"""isNotifiable"": {
                        (type.Implements<INotifyPropertyChanged>() ? "true" : "false")}"
                ]
            }]}
}},
""qt"": {{
    {jsonProp[new(QtInfo, prop)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""type"": ""{exportType.MFn(Ns | Name | Arg)}"""
                ]
            }]}
}}";
            return Ok;
        }
    }
}
