/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Generator
{
    using Support;

    [TestClass]
    public class Test_DeclaringVsReflecting
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// When generating property X for class B, the generation rule will receive as source
        /// the PropertyInfo for B::X. The rule will need to determine if the property is
        /// "notifiable", i.e. if the type implements INotifyPropertyChanged. In this case, B::X is
        /// indeed notifiable, but A::X is not. If the generation rule uses DeclaringType instead of
        /// ReflectingType to determine if the property is notifiable, it will obtain the Type info
        /// for A instead of B, and will incorrectly not generate change notifications for B::X.
        /// </summary>
        public const string Source = """
            using System.ComponentModel;
            namespace Test {
                public class A
                {
                    public int X { get; set; }
                }
                public class B : A, INotifyPropertyChanged
                {
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task DeclaringVsReflecting()
        {
            var result = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            // Class `A` header is generated
            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/hpp/test/a.h", out var aHpp)
                // Property `X` is generated
                && Regex.IsMatch(aHpp, @"Q_PROPERTY\(qint32 x [^\)]*\)")
                // Property `X` is not notifiable
                && !Regex.IsMatch(aHpp, @"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)"));

            // Class `B` header is generated
            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/hpp/test/b.h", out var bHpp)
                // Property `X` is generated and notifiable
                && Regex.IsMatch(bHpp, @"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)"));
        }
    }
}
