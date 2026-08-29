// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    public class Function : CallGroup
    {
        public SortedSet<Function> Callers { get; internal set; }
        public SortedSet<Function> Called { get; internal set; }

        public override Call Caller
        {
            get => Callers?.SingleOrDefault();
            internal set { }
        }

        public new bool Thread => false;

        public override string this[Column column] => column switch
        {
            Column.Caller when Callers is { Count: 1 } => base[column],
            Column.Caller when Callers is { Count: > 1 }
                => $"Avg. {Ratio:0.0%} of {Callers.Count} callers",
            _ => base[column]
        };
    }
}
