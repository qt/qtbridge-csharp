// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

namespace MTest_DynamicObject
{
    [Qt.Ignore]
    public class Foo
    {
        public int IntProperty { get; set; }

        public int IntReadOnlyProperty => IntProperty;

        public int IntWriteOnlyProperty { set => IntProperty = value; }

        public string StringProperty { get; set; }

        public DateTime DateTimeProperty { get; set; } = new(1995, 5, 20);

        public Uri UriProperty { get; set; }
            = new("https://www.qt.io/development/qt-framework/qt-bridges");

        public ulong UInt64FuncInt(int n)
        {
            if (n == 0)
                return 0;
            if (n == 1)
                return 1;
            ulong fN = 1, fN_1 = 1, fN_2 = 0;
            for (int i = 2; i < n; i++) {
                fN_2 = fN_1;
                fN_1 = fN;
                fN = fN_1 + fN_2;
            }
            return fN;
        }
    }
}
