// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Reflection;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
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

    public class FortyTwo
    {
        public int Value()
        {
            Console.WriteLine("Hello World from C#!");
            return 42;
        }
    }
}
