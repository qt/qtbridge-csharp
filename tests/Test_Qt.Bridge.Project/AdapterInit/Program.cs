// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace AdapterInit
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("AdapterInit managed app ready");
            return 0;
        }
    }

    public static class FortyTwo
    {
        public static int Value => 42;
    }
}
