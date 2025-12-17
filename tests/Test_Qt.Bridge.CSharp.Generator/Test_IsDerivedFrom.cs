/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.Bridge.Extensions;

    [TestClass]
    public class Test_IsDerivedFrom
    {
        [TestMethod
            , DataRow(true, typeof(List<int>), "System.Collections.Generic.List`1")
            , DataRow(true, typeof(List<int>), "System.Collections.Generic.IEnumerable`1")
            , DataRow(false, typeof(int[]), "System.Collections.Generic.List`1")
            , DataRow(true, typeof(int[]), "System.Collections.Generic.IEnumerable`1")
        ]
        public void IsDerivedFrom(bool ok, object x, object y)
        {
            Type type = x switch { Type t => t, string s => Type.GetType(s), _ => null }
                ?? throw new ArgumentException(nameof(x));
            Type baseType = y switch { Type t => t, string s => Type.GetType(s), _ => null }
                ?? throw new ArgumentException(nameof(y));
            if (ok)
                Assert.IsTrue(type.IsDerivedFrom(baseType));
            else
                Assert.IsFalse(type.IsDerivedFrom(baseType));
        }
    }
}
