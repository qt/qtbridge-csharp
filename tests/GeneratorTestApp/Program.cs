/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace GeneratorTestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Escape)
                if (!Console.KeyAvailable)
                    Thread.Sleep(100);
        }
    }
}
