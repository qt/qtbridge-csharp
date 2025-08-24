/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using UserViewLib;

namespace UserViewCli
{
    internal class Program
    {
        public static IUserList Users { get; private set; } = new UserList();

        static void Main(string[] args)
        {
            Console.WriteLine("Fetching random users...");
            RandomUserService.Fetch(20)
                .OrderBy(x => x, UserComparer.ByLastName)
                .ToList()
                .ForEach(x => Users.Add(x));
            ConsoleExt.ClearScreen(false);
            PrintUsers();

            var rand = new Random();
            while (!ConsoleExt.EscapePressed) {
                Thread.Sleep(100);
                var w = rand.Next(100);
                if (w < 10) {
                    var removeIndex = rand.Next(Users.Count);
                    PrintUsers(removeIndex, ConsoleColor.Red);
                    Users.RemoveAt(removeIndex);
                    PrintUsers();
                }
                else if (w < 20) {
                    var newUser = RandomUserService.Fetch();
                    var index = Users.BinarySearch(newUser, UserComparer.ByLastName);
                    if (index < 0) {
                        Users.Add(newUser, ~index);
                        PrintUsers(~index, ConsoleColor.Green);
                        PrintUsers();
                    }
                }
            }
            ConsoleExt.ClearScreen(false);
        }

        private static void PrintUsers(int hlIndex = -1, ConsoleColor hlColor = ConsoleColor.Black)
        {
            ConsoleExt.ClearScreen();
            var users = Users
                .Select(x => $@"{x.Name.Full} ({x.Age}) ({x.Email})")
                .ToList();
            for (int i = 0; i < users.Count; i++) {
                if (i == hlIndex)
                    Console.ForegroundColor = hlColor;
                ConsoleExt.WriteLine(users[i]);
                if (i == hlIndex)
                    Console.ResetColor();
            }
            Thread.Sleep(500);
        }
    }
}
