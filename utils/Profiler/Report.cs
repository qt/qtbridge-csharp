// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Text;

namespace Qt.Bridge.Utils.Profiler
{
    public enum Column { Name, Caller, Calls, Average, Total }

    internal static class ProfilerReportExtensions
    {
        public static int[] ColumnWidths(this IList<Call> calls, params Column[] columns)
        {
            var result = new int[columns.Length];
            for (var i = 0; i < columns.Length; ++i)
                result[i] = calls.Max(call => call[columns[i]].Length);
            return result;
        }

        private const string LeftBorder = "| ";
        private const string RightBorder = " |";
        private const string VerticalSeparator = " | ";
        private const char HorizontalSeparator = '-';

        public static string Report(this IEnumerable<Call> calls, params Column[] columns)
        {
            var grid = calls
                .Select(call => columns.Select(column => call[column]).ToArray())
                .ToList();

            var colWidths = columns
                .Select((_, idx) => grid
                    .Append([.. columns.Select(column => $"{column}")])
                    .Max(row => row[idx].Length))
                .ToArray();

            var report = new StringBuilder();
            report
                .Append(LeftBorder)
                .AppendJoin(VerticalSeparator, columns
                    .Select((column, idx) => column.ToString().PadRight(colWidths[idx])))
                .Append(RightBorder)
                .AppendLine();
            report
                .Append(LeftBorder.Replace(' ', HorizontalSeparator))
                .AppendJoin(VerticalSeparator.Replace(' ', HorizontalSeparator), columns
                    .Select((_, idx) => new string(HorizontalSeparator, colWidths[idx])))
                .Append(RightBorder.Replace(' ', HorizontalSeparator))
                .AppendLine();
            grid.ForEach(row => report
                .Append(LeftBorder)
                .AppendJoin(VerticalSeparator, row
                    .Select((cell, idx) => cell.PadRight(colWidths[idx])))
                .Append(RightBorder)
                .AppendLine());

            return report.ToString();
        }
    }

}
