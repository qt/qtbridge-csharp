/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace UserViewCli
{
    internal static class ConsoleExt
    {
        public static void WriteLine(string text)
        {
            Console.Write(text);
            var pos = Console.GetCursorPosition();
            Console.WriteLine(new string(' ', Console.WindowWidth - pos.Left));
        }

        public static bool EscapePressed
            => Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape;

        public static void ClearScreen(bool keepContent = true)
        {
            Console.CursorVisible = false;
            if (!keepContent)
                Console.SetCursorPosition(0, 0);
            var pos = Console.GetCursorPosition();
            Console.WriteLine(new string(' ', Console.WindowWidth - pos.Left));
            for (int i = 0; i < Console.WindowHeight - pos.Top - 1; i++)
                Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
        }
    }
}
