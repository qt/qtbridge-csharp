// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Qt.Bridge.Models;
using Qt.DotNet;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_ModelRemoveRanges
{
    using System.Linq;

    internal class Program
    {
        static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }

    public class RemoveRangeTableModel : TableModel<string>
    {
        private List<List<string>> Items { get; } = Enumerable.Range(0, 10)
            .Select(row => Enumerable.Range(0, 10)
                .Select(col => $"R{row}C{col}")
                .ToList())
            .ToList();

        public int LastRowRemoveFirst { get; private set; } = -1;
        public int LastRowRemoveLast { get; private set; } = -1;
        public int LastColumnRemoveFirst { get; private set; } = -1;
        public int LastColumnRemoveLast { get; private set; } = -1;

        public RemoveRangeTableModel()
        {
            ModelChanged += (_, args) =>
            {
                if (args.Action == EventAction.BeginRemoveRows) {
                    LastRowRemoveFirst = args.First;
                    LastRowRemoveLast = args.Last;
                }

                if (args.Action == EventAction.BeginRemoveColumns) {
                    LastColumnRemoveFirst = args.First;
                    LastColumnRemoveLast = args.Last;
                }
            };
        }

        protected override int Rows => Items.Count;
        protected override int Columns => Items.Count > 0 ? Items[0].Count : 0;

        protected override string this[int row, int col]
        {
            get => Items[row][col];
            set => Items[row][col] = value;
        }

        protected override bool CanRemoveRows(int row, int count)
            => row >= 0 && count > 0 && row + count <= Items.Count;

        protected override bool RemoveRows(int row, int count)
        {
            if (!CanRemoveRows(row, count))
                return false;
            Items.RemoveRange(row, count);
            return true;
        }

        protected override bool CanRemoveColumns(int column, int count)
            => column >= 0 && count > 0 && column + count <= Columns;

        protected override bool RemoveColumns(int column, int count)
        {
            if (!CanRemoveColumns(column, count))
                return false;
            foreach (var row in Items)
                row.RemoveRange(column, count);
            return true;
        }

        public void ResetLastRemoveRanges()
        {
            LastRowRemoveFirst = -1;
            LastRowRemoveLast = -1;
            LastColumnRemoveFirst = -1;
            LastColumnRemoveLast = -1;
        }

        public bool RemoveRowsViaModelApi(int row, int count)
            => RemoveRows(row, count, ModelIndex.Empty);

        public bool RemoveColumnsViaModelApi(int column, int count)
            => RemoveColumns(column, count, ModelIndex.Empty);
    }
}
