// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using UserViewLib;

namespace UserViewCli
{
    internal class Program
    {
        public static IUserList Users { get; private set; } = new UserList();

        static void Main(string[] args)
        {
            Console.WriteLine("Fetching random users...");
            Users.AddRange(RandomUserService.Fetch(20)
                .OrderBy(x => x, UserComparer.ByLastName)
                .ToList());
            ConsoleExt.ClearScreen(false);
            PrintUsers();

            var rand = new Random();
            bool escPressed = false;
            while (!escPressed) {
                if (Console.KeyAvailable) {
                    switch (Console.ReadKey(true).Key) {
                        case ConsoleKey.Escape:
                            escPressed = true;
                            continue;
                        case ConsoleKey.Enter:
                            AddUser();
                            break;
                        case ConsoleKey.Backspace:
                            RemoveUser(rand.Next(Users.Count));
                            break;
                    }
                    while (Console.KeyAvailable)
                        Console.ReadKey(true);
                }
                Thread.Sleep(100);
                var w = rand.Next(100);
                if (w < 10)
                    RemoveUser(rand.Next(Users.Count));
                else if (w < 20)
                    AddUser();
            }
            ConsoleExt.ClearScreen(false);
        }

        private static void AddUser()
        {
            var newUser = RandomUserService.Fetch();
            var index = Users.BinarySearch(newUser, UserComparer.ByLastName);
            if (index < 0) {
                Users.Add(newUser, ~index);
                PrintUsers(~index, ConsoleColor.Green);
                PrintUsers();
            }
        }

        private static void RemoveUser(int removeIndex)
        {
            PrintUsers(removeIndex, ConsoleColor.Red);
            Users.RemoveAt(removeIndex);
            PrintUsers();
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
            Console.Title = $"Users: {users.Count}";
            Thread.Sleep(500);
        }
    }
}
