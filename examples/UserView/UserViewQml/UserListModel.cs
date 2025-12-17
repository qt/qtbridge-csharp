/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections;
using System.ComponentModel;
using Qt.Bridge.Models;
using Qt.DotNet;
using UserViewLib;

namespace UserViewQml
{
    public class UserListModel : ListModel, IUserList, INotifyPropertyChanged
    {
        public enum UserItemRoles
        {
            FullName = Roles.UserRole,
            FirstName, LastName, Email, Thumbnail, Picture, BirthDate, Age
        }
        private readonly object CriticalSection = new();

        public event PropertyChangedEventHandler PropertyChanged;

        private UserList Users { get; set; } = new();

        public int Count { get; private set; } = 0;

        public void Add(User user, int index = -1)
        {
            if (user == null)
                return;
            if (index < 0 || index > Count)
                index = Count;
            BeginInsertRows(ModelIndex.Empty, index, index);
            Users.Add(user, index);
            Count = Users.Count;
            EndInsertRows();
            PropertyChanged?.Invoke(this, new(nameof(Count)));
        }

        public void AddRange(IList<User> users, int index = -1)
        {
            if (users is null or { Count: 0 })
                return;
            if (index < 0 || index > Count)
                index = Count;
            BeginInsertRows(ModelIndex.Empty, index, index + users.Count - 1);
            foreach (var user in users)
                Users.Add(user, index++);
            Count = Users.Count;
            EndInsertRows();
            PropertyChanged?.Invoke(this, new(nameof(Count)));
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                return;
            BeginRemoveRows(ModelIndex.Empty, index, index);
            Users.RemoveAt(index);
            Count = Users.Count;
            EndRemoveRows();
            PropertyChanged?.Invoke(this, new(nameof(Count)));
        }

        public User RemoveRandom(Random random)
        {
            User oldUser = null;
            if (!Users.Any())
                return null;
            int index = random.Next(Users.Count);
            if (index < 0 || index >= Count)
                return null;
            oldUser = Users.ElementAt(index);
            BeginRemoveRows(ModelIndex.Empty, index, index);
            Users.RemoveAt(index);
            Count = Users.Count;
            EndRemoveRows();
            PropertyChanged?.Invoke(this, new(nameof(Count)));
            return oldUser;
        }

        public int BinarySearch(User user, IComparer<User> comparer)
        {
            return Users.BinarySearch(user, comparer);
        }

        public IEnumerator<User> GetEnumerator()
        {
            return Users.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Users.GetEnumerator();
        }

        public Dictionary<int, string> RoleMap { get; } = new()
        {
            { (int)UserItemRoles.FullName, "fullName" },
            { (int)UserItemRoles.FirstName, "firstName" },
            { (int)UserItemRoles.LastName, "lastName" },
            { (int)UserItemRoles.Email, "email" },
            { (int)UserItemRoles.Thumbnail, "thumbnail" },
            { (int)UserItemRoles.Picture, "picture" },
            { (int)UserItemRoles.BirthDate, "birthDate" },
            { (int)UserItemRoles.Age, "age" }
        };

        public override Dictionary<int, string> RoleNames()
        {
            return RoleMap;
        }

        public override object Data(ModelIndex index, int role)
        {
            var users = Users.ToList();
            if (index.Row >= users.Count)
                return null;
            var user = users.ElementAt(index.Row);
            return role switch
            {
                (int)Roles.DisplayRole or
                (int)UserItemRoles.FullName => user.Name.Full,
                (int)UserItemRoles.FirstName => user.Name.First,
                (int)UserItemRoles.LastName => user.Name.Last,
                (int)UserItemRoles.Email => user.Email,
                (int)UserItemRoles.Thumbnail => user.Picture.Thumbnail,
                (int)UserItemRoles.Picture => user.Picture.Large,
                (int)UserItemRoles.BirthDate => user.Birth.Date,
                (int)UserItemRoles.Age => user.Age,
                _ => null
            };
        }

        public override int RowCount(ModelIndex parent)
        {
            return Count;
        }
    }
}
