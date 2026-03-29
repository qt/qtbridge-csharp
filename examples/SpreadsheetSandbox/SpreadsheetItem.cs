// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using ClosedXML.Excel;
using Qt.Bridge.Models;

namespace SpreadsheetSandbox
{
    public interface ISpreadsheetItem : IDisplayable, IEditable
    { }

    public sealed partial class SpreadsheetModel
    {
        private class SpreadsheetItem : ISpreadsheetItem
        {
            public SpreadsheetModel Model { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
            private IXLCell Cell => Model.Sheet.Cell(Row + 1, Column + 1);

            public object DisplayValue => Cell.CachedValue.ToString();

            public bool IsEditable => true;

            public object EditValue
            {
                get => Cell.HasFormula ? $"={Cell.FormulaA1}" : Cell.CachedValue.ToString();
                set
                {
                    var cellValue = value.ToString();
                    if (cellValue.StartsWith("="))
                        Cell.SetFormulaA1(cellValue.Substring(1));
                    else
                        Cell.SetValue(cellValue);
                    Model.CellChanged(Cell);
                }
            }
        }
    }
}
