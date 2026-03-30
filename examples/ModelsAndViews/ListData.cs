// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Bridge.Models;
using Qt.DotNet;

namespace ModelsAndViews
{
    public class ListData : Model
    {
        public List<string> Items { get; } =
        [
            "John",
            "Paul",
            "George",
            "Ringo"
        ];

        public override ModelIndex Parent(ModelIndex index) => ModelIndex.Empty;

        public override ModelIndex Index(int row, int column, ModelIndex parent)
        {
            if (parent?.IsValid == true)
                return ModelIndex.Empty;
            return new ModelIndex(row, column);
        }

        public override int RowCount(ModelIndex parent)
        {
            if (parent?.IsValid == true)
                return 0;
            return Items.Count;
        }

        public override int ColumnCount(ModelIndex parent) => 1;

        private static Dictionary<int, string> RoleNamesById { get; } = new()
        {
            { Roles.DisplayRole, "display" },
            { Roles.EditRole, "edit" }
        };
        public override Dictionary<int, string> RoleNames() => RoleNamesById;

        public override int Flags(ModelIndex index) => base.Flags(index) | ItemFlags.ItemIsEditable;

        public override object Data(ModelIndex index, int role)
        {
            if (index is not { IsValid: true })
                return null;
            if (index.Row < 0 || index.Row >= Items.Count)
                return null;
            return role switch
            {
                Roles.DisplayRole or Roles.EditRole => Items[index.Row],
                _ => null
            };
        }

        public override bool SetData(ModelIndex index, object value, int role)
        {
            if (index is not { IsValid: true })
                return false;
            if (index.Row < 0 || index.Row >= Items.Count)
                return false;
            Items[index.Row] = value.ToString();
            DataChanged(index, index, [Roles.DisplayRole, Roles.EditRole]);
            return true;
        }

        public override bool InsertRows(int row, int count, ModelIndex parent = null)
        {
            if (parent?.IsValid == true)
                return false;
            if (row < 0 || row > Items.Count)
                return false;
            if (count < 1)
                return false;
            BeginInsertRows(parent, row, row + count - 1);
            try {
                var newRows = Enumerable.Range(0, count)
                    .Select(_ => "(empty)");
                Items.InsertRange(row, newRows);
            } finally {
                EndInsertRows();
            }
            return true;
        }

        public override bool RemoveRows(int row, int count, ModelIndex parent = null)
        {
            if (parent?.IsValid == true)
                return false;
            if (row < 0 || row >= Items.Count)
                return false;
            if (count < 1 || row + count > Items.Count)
                return false;
            BeginRemoveRows(parent, row, row + count - 1);
            try {
                Items.RemoveRange(row, count);
            } finally {
                EndRemoveRows();
            }
            return true;
        }
    }
}
