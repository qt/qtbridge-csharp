// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.Utils.Profiler
{
    public class Scope
    {
        public string File { get; internal set; }
        public uint Line { get; internal set; }
        public string Tag { get; internal set; }
        public SortedSet<Call> Calls { get; internal set; }
        public SortedSet<CallGroup> CallGroups { get; internal set; }
        public SortedSet<Function> Functions { get; internal set; }
        public override string ToString() => Tag;
    }
}
