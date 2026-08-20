// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt;

[assembly: Export(Global = true, Options = ExportAs.SourceCode)]

namespace Qt.Bridge
{
    [Ignore]
    public class Program
    {
        public static void Main(string[] args) => throw new InvalidOperationException();
    }
}
