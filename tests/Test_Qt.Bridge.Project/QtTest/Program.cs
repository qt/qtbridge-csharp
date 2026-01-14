// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Reflection;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace QtTest
{
    internal class Program
    {
        static int Main(string[] args)
        {
            {
                Console.WriteLine("Hello World from C#!");
                return 0;
            }
        }
    }
    public static class FortyTwo
    {
        public static int Value => 42;
    }
}
