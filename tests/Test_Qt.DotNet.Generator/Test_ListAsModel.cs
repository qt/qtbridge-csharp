/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Generator
{
    using Support;

    [TestClass]
    public class Test_ListAsModel
    {
        public TestContext TestContext { get; set; }

        public const string Source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            namespace Test {
                public class Foo
                {
                    public ArrayList X { get; set; }
                    public List<string> Y { get; set; }
                    public ArraySegment<int> Z { get; set; }
                    public Foo[] W { get; set; }
                }
            }
            """;

        [TestMethod]
        public async Task ListAsModel()
        {
            var result = await TestCodeGenerator.GenerateAsync([Source],
                ct: TestContext.CancellationTokenSource.Token);
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/system/collections/arraylist.h", out var x) && Regex.IsMatch(x,
                @"class System::Collections::ArrayList\s+:\s+public\s+QAbstractListModel,"));
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/system/collections/generic/list.h", out var y) && Regex.IsMatch(y,
                @"class System::Collections::Generic::List_1__String\s+:\s+public\s+QAbstractListModel,"));
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/system/arraysegment.h", out var z) && Regex.IsMatch(z,
                @"class System::ArraySegment_1__Int32\s+:\s+public\s+QAbstractListModel,"));
            Assert.IsTrue(result.Sink.Files.TryGetValue(
                "source/hpp/test/array_foo.h", out var w) && Regex.IsMatch(w,
                @"class Test::Array_Foo\s+:\s+public\s+QAbstractListModel,"));
        }
    }
}
