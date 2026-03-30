// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Bridge.Models;
using Qt.DotNet;

namespace ModelsAndViews
{
    public class TableData : TableModel<string>
    {
        private List<List<string>> Items { get; } =
        [
            [ "John",   "Lennon"    ],
            [ "Paul",   "McCartney" ],
            [ "George", "Harrison"  ],
            [ "Ringo",  "Starr"     ],
        ];

        protected override int Rows => Items.Count;

        protected override int Columns => 2;

        protected override string RowHeader(int row) => $"{row + 1}";

        protected override string ColumnHeader(int column) => column switch
        {
            0 => "First Name",
            1 => "Last Name",
            _ => string.Empty
        };

        protected override string this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= Items.Count)
                    return null;
                var itemRow = Items[row];
                if (col < 0 || col >= itemRow.Count)
                    return null;
                return itemRow[col];
            }
            set
            {
                if (row < 0 || row >= Items.Count)
                    return;
                var itemRow = Items[row];
                if (col < 0 || col >= itemRow.Count)
                    return;
                itemRow[col] = value;
            }
        }

        protected override bool CanInsertRows(int row, int count)
        {
            if (row < 0 || row > Items.Count)
                return false;
            if (count < 1)
                return false;
            return true;
        }

        protected override bool InsertRows(int row, int count)
        {
            if (!CanInsertRows(row, count))
                return false;
            var newRows = Enumerable.Range(0, count)
                .Select(_ => Enumerable.Range(0, Columns)
                    .Select(_ => "(empty)")
                    .ToList());
            Items.InsertRange(row, newRows);
            return true;
        }

        protected override bool CanRemoveRows(int row, int count)
        {
            if (row < 0 || row >= Items.Count)
                return false;
            if (count < 1 || row + count > Items.Count)
                return false;
            return true;
        }

        protected override bool RemoveRows(int row, int count)
        {
            if (!CanRemoveRows(row, count))
                return false;
            Items.RemoveRange(row, count);
            return true;
        }
    }
}
