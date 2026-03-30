// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Reflection;
using Qt.Bridge.Models;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
    public sealed class TestTable : TableModel<int>
    {
        private int _Rows = 2;
        protected override int Rows => _Rows;

        private int _Columns = 2;
        protected override int Columns => _Columns;

        protected override int this[int row, int col]
        {
            get => col - row;
            set { }
        }

        protected override bool CanInsertRows(int row, int count) => row >= 0 && count > 0;
        protected override bool InsertRows(int row, int count)
        {
            if (!CanInsertRows(row, count))
                return false;
            _Rows = Math.Max(_Rows, row) + count;
            return true;
        }

        protected override bool CanInsertColumns(int column, int count) => column >= 0 && count > 0;
        protected override bool InsertColumns(int column, int count)
        {
            if (!CanInsertColumns(column, count))
                return false;
            _Columns = Math.Max(_Columns, column) + count;
            return true;
        }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            {
                Qml.WaitForExit();
                return 0;
            }
        }
    }
}
