/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections;

namespace UserViewLib
{
    public interface IUserList : IEnumerable<User>
    {
        int Count { get; }
        void Add(User user, int index = -1);
        void AddRange(IList<User> users, int index = -1);
        void RemoveAt(int index);
        int BinarySearch(User user, IComparer<User> comparer);
    }

    public class UserList : IUserList
    {
        private List<User> Users { get; set; } = [];

        public int Count => Users.Count;

        public void Add(User user, int index = -1)
        {
            if (user == null)
                return;
            if (index < 0 || index > Users.Count)
                index = Users.Count;
            Users.Insert(index, user);
        }

        public void AddRange(IList<User> users, int index = -1)
        {
            if (users is null or { Count: 0 })
                return;
            if (index < 0 || index > Users.Count)
                index = Users.Count;
            foreach (User user in users)
                Users.Insert(index++, user);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Users.Count)
                return;
            Users.RemoveAt(index);
        }

        public int BinarySearch(User user, IComparer<User> comparer)
        {
            if (user == null)
                return ~Users.Count;
            return Users.BinarySearch(user, comparer);
        }

        public IEnumerator<User> GetEnumerator() => Users.ToList().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
