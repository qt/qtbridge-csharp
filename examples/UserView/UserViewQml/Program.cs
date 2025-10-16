/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.Quick;
using UserViewLib;

namespace UserViewQml
{
    internal class Program
    {
        public static IUserList Users { get; set; }

        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("Main");

            bool listInit = false;
            var rand = new Random();
            while (!Qml.WaitForExit(100)) {
                if (Users == null)
                    continue;
                if (!listInit) {
                    listInit = true;
                    Users.AddRange(RandomUserService.Fetch(20)
                        .OrderBy(x => x, UserComparer.ByLastName)
                        .ToList());
                    continue;
                }
                var w = rand.Next(100);
                if (w < 10) {
                    var removeIndex = rand.Next(Users.Count);
                    Users.RemoveAt(removeIndex);
                } else if (w < 20) {
                    var newUser = RandomUserService.Fetch();
                    var index = Users.BinarySearch(newUser, UserComparer.ByLastName);
                    if (index < 0)
                        Users.Add(newUser, ~index);
                }
            }
        }
    }
}
