/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.ComponentModel;
using UserViewLib;

using Qt.DotNet;
using Qt.DotNet.Utils;
using Qt.MetaObject;
using Qt.Quick;

namespace UserViewQml
{
    public class UserEventArgs : EventArgs
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public User User { get; set; }
    }

    public class UserViewApp
    {
        public UserListModel Users { get; private set; } = new();

        public UserViewApp()
        {
            Program.Users = Users;
        }

        public event EventHandler<UserEventArgs> UserAdded;
        public event EventHandler<UserEventArgs> UserRemoved;

        public void Add()
        {
            if (RandomUserService.Fetch(1).FirstOrDefault() is not { } newUser)
                return;
            var index = Users.BinarySearch(newUser, UserComparer.ByLastName);
            if (index < 0) {
                Users.Add(newUser, ~index);
                UserAdded?.Invoke(this, new UserEventArgs { User = newUser });
            }
        }

        public void Remove()
        {
            if (Users.RemoveRandom(random) is not { } oldUser)
                return;
            UserRemoved?.Invoke(this, new UserEventArgs { User = oldUser });
        }

        private readonly Random random = new();
    }
}
