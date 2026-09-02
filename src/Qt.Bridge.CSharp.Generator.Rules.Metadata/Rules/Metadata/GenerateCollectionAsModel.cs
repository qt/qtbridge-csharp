// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Utils.Text;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    /// <summary>Exports list-shaped CLR types as dynamic QAbstractListModels.</summary>
    public sealed class GenerateCollectionAsModel : GenerateType
    {
        public override int Priority => base.Priority + 1;

        public override bool Matches(MemberInfo src) => src is Type type
            && type.IsList(out _) && type.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type || !type.IsList(out var itemType)
                || type.GetPlaceholder(QtInfo) is not { } qtInfo) {
                return Error();
            }

            var isArray = type.IsArray;
            var properties = itemType.IsValue()
                ? []
                : itemType.GetProperties()
                    .Where(property => !property.IsStatic())
                    .Where(property => property.GetIndexParameters().Length == 0)
                    .Where(property => property.GetGetMethod() != null)
                    .Where(property => !property.IsIgnored())
                    .ToArray();

            var roles = new List<(string DotNetName, string QtName)> { ("", "item") };
            var usedNames = new HashSet<string>(StringComparer.Ordinal) { "item" };
            foreach (var property in properties) {
                var name = property.Name.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel);
                var candidate = name;
                for (var suffix = 2; !usedNames.Add(candidate); ++suffix)
                    candidate = name + suffix;
                roles.Add((property.Name, candidate));
            }

            qtInfo += $@"
""model"": {{
    {qtInfo[new(QtModelInfo, type)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    "\"baseClass\": \"listModel\"",
                    $@"""collection"": {{
    ""countMethod"": ""{(isArray ? "get_Length" : "get_Count")}"",
    ""itemMethod"": ""{(isArray ? "Get" : "get_Item")}"",
    ""isObservable"": {(type.IsObservableList(out _) ? "true" : "false")},
    ""roles"": [
{string.Join(",\r\n", roles.Select(role => $@"        {{ ""dotNet"": {{ ""name"": ""{role.DotNetName}"" }}, ""qt"": {{ ""name"": ""{role.QtName}"" }} }}"))}
    ]
}}"
                ]
            }]}
}}";
            return Ok;
        }
    }
}
