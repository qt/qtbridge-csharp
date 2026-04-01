// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;
using ClosedXML.Excel;
using Qt.Bridge.Models;
using Qt.Quick;

namespace SpreadsheetSandbox
{
    using RC = (int Row, int Column);

    [QmlElement(Name = "Spreadsheet", Singleton = true)]
    public sealed partial class SpreadsheetModel
        : TableModel<ISpreadsheetItem>
        , INotifyPropertyChanged
    {
        private XLWorkbook Workbook { get; set; }
        private IXLWorksheet Sheet => Workbook?.Worksheet(1);

        public event PropertyChangedEventHandler PropertyChanged;

        public string FileName { get; private set; }

        public SpreadsheetModel()
        {
            Workbook = new();
            // Add default spreadsheet
            Workbook.AddWorksheet()
                // Extend to 100x100
                .Cell(100, 100).Value = " ";
        }

        public bool OpenFile(Uri uri)
        {
            if (!File.Exists(uri.LocalPath))
                return false;

            XLWorkbook newWorkbook;
            try {
                newWorkbook = new XLWorkbook(uri.LocalPath);
            } catch (Exception) {
                return false;
            }
            if (!newWorkbook.Worksheets.Any()) {
                newWorkbook.Dispose();
                return false;
            }

            BeginResetModel();
            Workbook?.Dispose();
            Workbook = newWorkbook;
            // Extend spreadsheet to min. 100x100
            Sheet.Cell(int.Max(100, Rows + 1), int.Max(100, Columns + 1)).Value = " ";
            EndResetModel();

            FileName = Path.GetFileName(uri.LocalPath);
            PropertyChanged?.Invoke(this, new(nameof(FileName)));

            return true;
        }

        // Needed because QAIM::clearItemData() is not exposed to QML
        public bool ClearCell(int row, int col) => ClearItemData(row, col);

        protected override int Rows => Sheet.LastRowUsed()?.RowNumber() ?? 0;

        protected override int Columns => Sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        protected override ISpreadsheetItem this[int row, int col]
        {
            get => new SpreadsheetItem() { Model = this, Row = row, Column = col };
            set => throw new InvalidOperationException();
        }

        protected override string RowHeader(int row) => Sheet.Row(row + 1).RowNumber().ToString();

        protected override string ColumnHeader(int column) => Sheet.Column(column + 1).ColumnLetter();

        protected override bool ClearItemData(int row, int col)
        {
            var cell = Sheet.Cell(row + 1, col + 1);
            cell.SetValue(Blank.Value);
            CellChanged(cell);
            return true;
        }

        protected override bool CanInsertRows(int row, int count)
            => row >= 0 && row <= Rows && count >= 1;

        protected override bool CanInsertColumns(int column, int count)
            => column >= 0 && column <= Columns && count >= 1;

        protected override bool CanRemoveRows(int row, int count)
            => row >= 0 && row < Rows && count >= 1;

        protected override bool CanRemoveColumns(int column, int count)
            => column >= 0 && column < Columns && count >= 1;

        protected override bool InsertRows(int row, int count)
        {
            if (!CanInsertRows(row, count))
                return false;

            try {
                Sheet.Row(row + 1).InsertRowsAbove(count);
                Workbook.RecalculateAllFormulas();
            } catch (Exception) {
                return false;
            }
            foreach (var rc in UpdateFormulas())
                DataChanged(rc.Row, rc.Column);
            return true;
        }

        protected override bool InsertColumns(int column, int count)
        {
            if (!CanInsertColumns(column, count))
                return false;

            try {
                Sheet.Column(column + 1).InsertColumnsBefore(count);
            } catch (Exception) {
                return false;
            }
            foreach (var rc in UpdateFormulas())
                DataChanged(rc.Row, rc.Column);
            return true;
        }

        protected override bool RemoveRows(int row, int count)
        {
            if (!CanRemoveRows(row, count))
                return false;

            try {
                Sheet.Row(row + 1).Delete();
            } catch (Exception) {
                return false;
            }
            foreach (var rc in UpdateFormulas())
                DataChanged(rc.Row, rc.Column);
            return true;
        }

        protected override bool RemoveColumns(int column, int count)
        {
            if (!CanRemoveColumns(column, count))
                return false;

            try {
                Sheet.Column(column + 1).Delete();
            } catch (Exception) {
                return false;
            }
            foreach (var rc in UpdateFormulas())
                DataChanged(rc.Row, rc.Column);
            return true;
        }

        private RC Location(IXLCell cell)
            => (cell.WorksheetRow().RowNumber() - 1, cell.WorksheetColumn().ColumnNumber() - 1);

        private RC Location(IXLRow row) => (row.RowNumber() - 1, -1);

        private RC Location(IXLColumn col) => (-1, col.ColumnNumber() - 1);

        private void CellChanged(IXLCell cell)
        {
            var updatedCells = UpdateFormulas();
            updatedCells.Add(Location(cell));
            foreach (var rc in updatedCells)
                DataChanged(rc.Row, rc.Column);
        }

        private HashSet<RC> UpdateFormulas()
        {
            var updatedFormulas = new HashSet<RC>();
            var formulaCells = Workbook.FindCells(c => c.Worksheet == Sheet && c.HasFormula)
                .ToDictionary(c => Location(c), c => c.CachedValue.ToString());
            Workbook.RecalculateAllFormulas();
            foreach (var c in formulaCells) {
                var formulaCell = Sheet.Cell(c.Key.Row + 1, c.Key.Column + 1);
                var formulaValue = formulaCell.CachedValue.ToString();
                if (c.Value != formulaValue)
                    updatedFormulas.Add(c.Key);
            }
            return updatedFormulas;
        }
    }
}
