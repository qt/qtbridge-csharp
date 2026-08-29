// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    public class Call : Scope
    {
        public Scope Scope { get; internal set; }
        public long Thread { get; internal set; }
        public long Start { get; internal set; }
        public long Stop { get; internal set; }
        public double Total { get; internal set; }
        public virtual Call Caller { get; internal set; }
        public double Ratio { get; internal set; }

        public class Comparer<T> : IComparer<T> where T : Call
        {
            public Func<T, IComparable> OrderBy { get; set; } = f => f.Start;
            public bool Desc { get; set; } = false;

            public int Compare(T x, T y)
            {
                return (x, y) switch
                {
                    (null, { }) => -1,
                    ({ }, null) => 1,
                    _ when x == y => 0,
                    _ => OrderBy(x).CompareTo(OrderBy(y)) switch
                    {
                        0 => x.Start.CompareTo(y.Start) switch
                        {
                            0 => 1,
                            var result => result,
                        },
                        var result => result
                    }
                }
                switch
                {
                    -1 when Desc => 1,
                    1 when Desc => -1,
                    var result => result,
                };
            }
        }

        protected static string StrNSecs(double n) => Math.Abs(n) switch
        {
            0 => "0",
            < 1000 => $"{n:0 ns}",
            < 1000000 => $"{n:0 us ### ns}",
            < 1000000000 => $"{n:0 ms ### us ### ns}",
            _ => $"{n:0 s ### ms ### us ### ns}"
        };

        public string Name => Scope?.Tag switch
        {
            { Length: > 0 } => $"{Scope.Tag}::{Tag}({Line})",
            _ => $"{File}::{Tag}({Line})"
        };

        public virtual string this[Column column] => column switch
        {
            Column.Name => Name,
            Column.Total => StrNSecs(Total),
            Column.Caller when Caller is { } => $"{Ratio:0.0%} x {Caller.Name}",
            _ => ""
        };

        public override string ToString()
        {
            return string.Join(", ", Enum.GetValues<Column>()
                .Select(column => this[column] switch
                {
                    { Length: > 0 } value => $"{column}=[{value}]",
                    _ => null
                })
                .Where(value => value != null));
        }
    }
}
