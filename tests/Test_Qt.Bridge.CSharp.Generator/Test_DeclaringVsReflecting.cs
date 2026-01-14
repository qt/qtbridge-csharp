// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    /// <summary>
    /// When generating property X for class B, the generation rule will receive as source
    /// the PropertyInfo for B::X. The rule will need to determine if the property is
    /// "notifiable", i.e. if the type implements INotifyPropertyChanged. In this case, B::X is
    /// indeed notifiable, but A::X is not. If the generation rule uses DeclaringType instead of
    /// ReflectingType to determine if the property is notifiable, it will obtain the Type info
    /// for A instead of B, and will incorrectly not generate change notifications for B::X.
    /// </summary>
    [TestClass]
    public class Test_DeclaringVsReflecting
    {
        public TestContext TestContext { get; set; }

        private const string Source = """
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

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/a.h", out var aHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]*\)", aHpp);
            Assert.DoesNotMatchRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", aHpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/b.h", out var bHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", bHpp);
        }

        private const string OverrideSources = """
            using System.ComponentModel;
            namespace Test {
                public class A
                {
                    public virtual int X { get; set; }
                }
                public class B : A, INotifyPropertyChanged
                {
                    public override int X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task OverridenProperty_UsesReflectedTypeForNotifiability()
        {
            var result = await TestCodeGenerator.GenerateAsync([OverrideSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/a.h", out var aHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]*\)", aHpp);
            Assert.DoesNotMatchRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", aHpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/b.h", out var bHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", bHpp);
        }

        private const string TwoLevelOverrideSources = """
            using System.ComponentModel;
            namespace Test
            {
                public class A
                {
                     public virtual int X { get; set; }
                }
                public class B : A
                {
                    public override int X { get; set; }
                }
                public class C : B, INotifyPropertyChanged
                {
                    public override int X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task MultiLevelOverride_OnlyDerivedWithINotifyIsNotifiable()
        {
            var result = await TestCodeGenerator.GenerateAsync([TwoLevelOverrideSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/a.h", out var aHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]*\)", aHpp);
            Assert.DoesNotMatchRegex(@"NOTIFY xChanged\)", aHpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/b.h", out var bHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]*\)", bHpp);
            Assert.DoesNotMatchRegex(@"NOTIFY xChanged\)", bHpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/c.h", out var cHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", cHpp);
        }

        private const string ShadowSources = """
            using System.ComponentModel;
            namespace Test {
                public class A
                {
                    public int X { get; set; }
                }
                public class B : A, INotifyPropertyChanged
                {
                    public new int X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
                public class C : A, INotifyPropertyChanged
                {
                    public new float X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task ShadowedProperty_UsesReflectedTypeForNotifiability()
        {
            var result = await TestCodeGenerator.GenerateAsync([ShadowSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/a.h", out var aHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]*\)", aHpp);
            Assert.DoesNotMatchRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", aHpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/b.h", out var bHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", bHpp);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/c.h", out var cHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\((?:qreal|float) x [^\)]* NOTIFY xChanged\)", cHpp);
        }

        private const string AbstractOverrideSources = """
            using System.ComponentModel;
            namespace Test
            {
                public abstract class A
                {
                    public abstract int X { get; set; }
                }
                public class B : A, INotifyPropertyChanged
                {
                    public override int X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task AbstractProperty_Override_IsNotifiableOnDerived()
        {
            var result = await TestCodeGenerator.GenerateAsync([AbstractOverrideSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/a.h", out var aHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]*\)", aHpp);
            Assert.DoesNotMatchRegex(@"NOTIFY xChanged\)", aHpp);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/b.h", out var bHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", bHpp);
        }

        private const string ExplicitInterfaceSources = """
            using System.ComponentModel;
            namespace Test {
                public interface IFoo
                {
                    int X { get; set; }
                }
                public class A : IFoo, INotifyPropertyChanged
                {
                    int IFoo.X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task ExplicitInterfaceProperty_NoQPropertyGenerated()
        {
            var result = await TestCodeGenerator.GenerateAsync([ExplicitInterfaceSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            // This must NOT generate a Q_PROPERTY:
            // In the source, class A implements IFoo.X *explicitly*: `int IFoo.X { get; set; }`.
            // Explicit interface implementations are non-public and do not appear on A's public API
            // (you can't access `a.X`; you must cast: `((IFoo)a).X`). Our generator intentionally
            // exposes only the reflected class's *public* instance properties to QML to mirror the
            // public surface and preserve encapsulation.

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/test/a.h", out var aHpp));
            Assert.DoesNotMatchRegex(@"\bQ_PROPERTY\(.+\bx\b", aHpp);
        }

        private const string ExplicitWithPublicForwarderSources = """
            using System.ComponentModel;
            namespace Test {
                public interface IFoo { int X { get; set; } }

                // Implements IFoo.X explicitly, and exposes a public forwarder 'X'.
                public class A : IFoo, INotifyPropertyChanged
                {
                    int IFoo.X { get; set; }  // explicit (non-public)
                    public int X               // public forwarder
                    {
                        get => ((IFoo)this).X;
                        set => ((IFoo)this).X = value;
                    }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task ExplicitInterface_WithPublicForwarder_IsGeneratedAndNotifiable()
        {
            var result = await TestCodeGenerator.GenerateAsync([ExplicitWithPublicForwarderSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/hpp/test/a.h", out var aHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)", aHpp);
        }

        private const string ImplicitInterfaceSources = """
            using System.ComponentModel;
            namespace Test {
                public interface IFoo { int X { get; set; } }

                // Implements IFoo.X implicitly (public).
                public class B : IFoo, INotifyPropertyChanged
                {
                    public int X { get; set; }
                    public event PropertyChangedEventHandler PropertyChanged;
                }
            }
            """;

        [TestMethod]
        public async Task ImplicitInterface_ImplicitPublicProperty_IsGeneratedAndNotifiable()
        {
            var result = await TestCodeGenerator.GenerateAsync([ImplicitInterfaceSources],
                sourceRefs: [typeof(INotifyPropertyChanged).Assembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue(@"source/hpp/test/b.h", out var bHpp));
            Assert.MatchesRegex(@"Q_PROPERTY\(qint32 x [^\)]* NOTIFY xChanged\)",bHpp);
        }
    }
}
