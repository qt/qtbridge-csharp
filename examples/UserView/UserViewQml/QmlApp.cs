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
    [QObject]
    [QmlElement(Singleton = true)]
    public class QmlApp : INotifyPropertyChanged
    {
        public QmlApp()
        {
            var users = RandomUserService.Fetch(3)
                .OrderBy(x => x, UserComparer.ByLastName);
            foreach (var user in users)
                Users.Add(user);
            resetAmountToAdd = new (_ => AmountToAdd = 1);
            lazy.PropertyChanged += Lazy_PropertyChanged;
        }

        [QSignal]
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { lazy.PropertyChanged += value; }
            remove { lazy.PropertyChanged -= value; }
        }

        [QProperty(Constant = true)]
        public UserListModel Users { get; } = new();

        public int AmountToAdd
        {
            get => lazy.Get(() => AmountToAdd, () => 1);
            set => lazy.Set(() => AmountToAdd, value);
        }

        [QSlot]
        public void Add()
        {
            _ = Task.Run(async () => await AddAsync());
        }

        [QSignal<UserSignal>]
        public event EventHandler<UserEventArgs> UserAdded;

        public async Task AddAsync()
        {
            var newUsers = await RandomUserService.FetchAsync(AmountToAdd);
            lock (criticalSection) {
                foreach (var newUser in newUsers.OrderBy(x => x, UserComparer.ByLastName)) {
                    var index = Users.BinarySearch(newUser, UserComparer.ByLastName);
                    if (index < 0) {
                        Users.Add(newUser, ~index);
                        UserAdded?.Invoke(this, new UserEventArgs { User = newUser });
                    }
                }
            }
        }

        [QSlot]
        public void Remove()
        {
            _ = Task.Run(async () => await RemoveAsync());
        }

        [QSignal<UserSignal>]
        public event EventHandler<UserEventArgs> UserRemoved;

        public async Task RemoveAsync()
        {
            await Task.Yield();
            User oldUser = null;
            lock (criticalSection) {
                if (!Users.Any())
                    return;
                int removeIndex = random.Next(Users.Count);
                oldUser = Users.ElementAt(removeIndex);
                Users.RemoveAt(removeIndex);
                UserRemoved?.Invoke(this, new UserEventArgs { User = oldUser });
            }
        }

        public class UserEventArgs : EventArgs
        {
            public DateTime Timestamp { get; } = DateTime.Now;
            public User User { get; set; }
        }

        public class UserSignal : Signal<UserEventArgs, string, string, int>
        {
            public override bool Convert(object sender, UserEventArgs args)
            {
                if (args.User == null)
                    return false;
                Param1 = args.User.Name.Full;
                Param2 = args.User.Picture.Large;
                Param3 = args.Timestamp.Subtract(DateTime.UnixEpoch).TotalSeconds;
                return true;
            }
        }

        private void Lazy_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(AmountToAdd))
                return;
            if (AmountToAdd > 1)
                resetAmountToAdd.Change(5000, Timeout.Infinite);
            else
                resetAmountToAdd.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private readonly LazyFactory lazy = new();
        private readonly Random random = new();
        private readonly object criticalSection = new ();
        private readonly Timer resetAmountToAdd;
    }
}
