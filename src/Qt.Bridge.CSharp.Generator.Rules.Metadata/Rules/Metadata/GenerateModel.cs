// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Models;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Placeholders;

    public class GenerateModel : GenerateType
    {
        public override int Priority => base.Priority + 1;

        public override bool Matches(MemberInfo src)
            => src is Type type && type.IsAssignableTo(TypeOf<Model>())
            && type.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            var modelTypes = new[]
            {
                (TypeOf<ListModel>(), "listModel"),
                (TypeOf<TableModel>(), "tableModel"),
                (TypeOf<Model>(), "model")
            };
            (var baseType, var baseClass) = modelTypes
                .FirstOrDefault(m => type.IsAssignableTo(m.Item1));

            var overrides = type.GetMethods()
                .Where(method => method.IsOverrideOf(baseType) && !method.IsIgnored())
                .Select(method => method.Name switch
                {
                    nameof(Model.RowCount) => "rowCount",
                    nameof(Model.ColumnCount) => "columnCount",
                    nameof(Model.RoleNames) => "roleNames",
                    nameof(Model.CanFetchMore) => "canFetchMore",
                    nameof(Model.Flags) => "flags",
                    nameof(Model.HasChildren) => "hasChildren",
                    nameof(Model.Index) => "index",
                    nameof(Model.Parent) => "parent",
                    nameof(Model.Sibling) => "sibling",
                    nameof(Model.Buddy) => "buddy",
                    nameof(Model.Data) => "data",
                    nameof(Model.HeaderData) => "headerData",
                    nameof(Model.InsertRows) => "insertRows",
                    nameof(Model.InsertColumns) => "insertColumns",
                    nameof(Model.MoveRows) => "moveRows",
                    nameof(Model.MoveColumns) => "moveColumns",
                    nameof(Model.RemoveRows) => "removeRows",
                    nameof(Model.RemoveColumns) => "removeColumns",
                    nameof(Model.Sort) => "sort",
                    nameof(Model.FetchMore) => "fetchMore",
                    nameof(Model.SetData) => "setData",
                    nameof(Model.SetHeaderData) => "setHeaderData",
                    _ => string.Empty
                })
                .Where(name => name is { Length: > 0 })
                .ToArray();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(QtInfo) is not { } qtInfo)
                return Error();

            qtInfo += $@"
""model"": {{
    {qtInfo[new(QtModelInfo, type)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""baseClass"": ""{baseClass}""",
                    overrides is not { Length: > 0 } ? string.Empty : $@"
""overrides"": [
{Tab}{string.Join($",\r\n{Tab}", overrides.Select(name => $@"""{name}"""))}
]
"
                ]
            }]}
}}";

            return Ok;
        }
    }
}
