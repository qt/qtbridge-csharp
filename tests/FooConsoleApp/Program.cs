/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace FooConsoleApp
{
    public static class Program
    {
        public static bool KeepRunning { get; set; } = true;
        public static int Main(string[] args)
        {
            Console.WriteLine("FooConsoleApp: started");
            Thread.Sleep(1000);
            while (KeepRunning) {
                Console.WriteLine("FooConsoleApp: running");
                Thread.Sleep(1000);
            }
            Console.WriteLine("FooConsoleApp: stopped");
            return 0;
        }
    }
}
