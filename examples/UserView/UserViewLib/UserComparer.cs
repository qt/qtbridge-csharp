// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

namespace UserViewLib
{
    public class UserComparer : IComparer<User>
    {
        public static UserComparer ByLastName { get; } = new()
        {
            Compare = (x, y) => string.Compare(x.Name.Last, y.Name.Last, true)
        };

        public static UserComparer ByFirstName { get; } = new()
        {
            Compare = (x, y) => string.Compare(x.Name.First, y.Name.First, true)
        };

        private UserComparer() { }
        private Func<User, User, int> Compare { get; set; }

        int IComparer<User>.Compare(User x, User y) => Compare(x, y);
    }
}
