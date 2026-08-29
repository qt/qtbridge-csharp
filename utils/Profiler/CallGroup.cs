// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    public class CallGroup : Call
    {
        public double Average { get; internal set; }

        public override string this[Column column] => column switch
        {
            Column.Calls => $"{Calls.Count:F0}",
            Column.Average => StrNSecs(Average),
            _ => base[column]
        };
    }
}
