/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.DotNet;
using System.Collections;
using UserViewLib;

namespace UserViewQml
{
    using static Adapter;

    public class UserListModel : QAbstractListModel, IUserList
    {
        private readonly object CriticalSection = new();
        private UserList Users { get; set; } = new();

        private IQModelIndex NullIndex { get; } = null;
        private IQModelIndex FirstIndex { get; } = null;

        public int Count { get; private set; } = 0;

        public UserListModel()
        {
            NullIndex = QModelIndex();
            FirstIndex = CreateIndex(0, 0, 0);
        }

        public void Add(User user, int index = -1)
        {
            if (user == null)
                return;
            if (index < 0 || index > Count)
                index = Count;
            BeginInsertRows(NullIndex, index, index);
            lock (CriticalSection) {
                Users.Add(user, index);
                Count = Users.Count;
            }
            EndInsertRows();
        }

        public void RemoveAt(int index)
        {
            BeginRemoveRows(NullIndex, index, index);
            lock (CriticalSection) {
                Users.RemoveAt(index);
                Count = Users.Count;
            }
            EndRemoveRows();
            EmitDataChanged(FirstIndex, FirstIndex, [(int)ItemDataRole.DisplayRole]);
        }

        public int BinarySearch(User user, IComparer<User> comparer)
        {
            lock (CriticalSection) {
                return Users.BinarySearch(user, comparer);
            }
        }

        public IEnumerator<User> GetEnumerator()
        {
            lock (CriticalSection) {
                return Users.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (CriticalSection) {
                return Users.GetEnumerator();
            }
        }

        public enum UserItemRoles
        {
            None = ItemDataRole.UserRole,
            FullName, FirstName, LastName, Email, Thumbnail, Picture
        }

        public override string RoleNames()
        {
            return "fullName,firstName,lastName,email,thumbnail,picture";
        }

        public override IQVariant Data(IQModelIndex index, int role = 0)
        {
            if (index.Row() >= Count)
                return null;
            var user = Users.ElementAt(index.Row());
            string text = role switch
            {
                (int)ItemDataRole.DisplayRole or
                (int)UserItemRoles.FullName => user.Name.Full,
                (int)UserItemRoles.FirstName => user.Name.First,
                (int)UserItemRoles.LastName => user.Name.Last,
                (int)UserItemRoles.Email => user.Email,
                (int)UserItemRoles.Thumbnail => user.Picture.Thumbnail,
                (int)UserItemRoles.Picture => user.Picture.Large,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(text))
                return null;
            var data = QVariant(text);
            return data;
        }

        public override int RowCount(IQModelIndex parent = null)
        {
            if (parent?.IsValid() == true)
                return 0;
            return Count;
        }
    }
}
