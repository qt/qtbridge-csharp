// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Bridge.Models;
using Qt.DotNet;

namespace ModelsAndViews
{
    public class TableData : Model
    {
        public List<string> Columns { get; } = ["First Name", "LastName"];

        public List<List<string>> Items { get; } =
        [
            [ "John",   "Lennon"    ],
            [ "Paul",   "McCartney" ],
            [ "George", "Harrison"  ],
            [ "Ringo",  "Starr"     ],
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

        public override int ColumnCount(ModelIndex parent) => Columns.Count;

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

            var row = Items[index.Row];
            if (index.Column < 0 || index.Column >= row.Count)
                return null;

            return role switch
            {
                Roles.DisplayRole or Roles.EditRole => row[index.Column],
                _ => null
            };
        }

        public override bool SetData(ModelIndex index, object value, int role)
        {
            if (index is not { IsValid: true })
                return false;
            if (index.Row < 0 || index.Row >= Items.Count)
                return false;

            var row = Items[index.Row];
            if (index.Column < 0 || index.Column >= row.Count)
                return false;

            if (role != Roles.EditRole)
                return false;

            row[index.Column] = value.ToString();
            DataChanged(index, index, [Roles.DisplayRole, Roles.EditRole]);
            return true;
        }

        public override object HeaderData(int section, int orientation, int role)
        {
            return (orientation, role) switch
            {
                (HeaderOrientation.Horizontal, Roles.DisplayRole)
                    when 0 <= section && section < Columns.Count => Columns[section],
                (HeaderOrientation.Vertical, Roles.DisplayRole)
                    when 0 <= section && section < Items.Count => (section + 1).ToString(),
                _ => null
            };
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
            var newRows = Enumerable.Range(0, count)
                .Select(_ => Enumerable.Range(0, Columns.Count)
                    .Select(_ => "(empty)")
                    .ToList());
            Items.InsertRange(row, newRows);
            EndInsertRows();
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
            Items.RemoveRange(row, count);
            EndRemoveRows();
            return true;
        }
    }
}
