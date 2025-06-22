/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using UserViewLib;

namespace UserViewQml
{
    internal class Program
    {
        public static IUserList Users { get; set; } = null;

        static void Main(string[] args)
        {
            while (true)
                Thread.Sleep(100);
        }
    }
}
