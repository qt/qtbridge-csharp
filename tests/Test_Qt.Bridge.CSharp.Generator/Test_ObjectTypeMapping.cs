// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_ObjectTypeMapping
    {
        public TestContext TestContext { get; set; } = null;
        private CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;

        [TestMethod]
        public async Task ArgTrait_QObjectDerived_ShouldBePointer()
        {
            const string src = """
                namespace MyApp
                {
                    public class MyOther
                    { }

                    public class UsesOther
                    {
                        public UsesOther() { }

                        public void acceptOther(MyOther o) // MyOther pointer expected
                        { }

                        public MyOther make() // MyOther pointer expected
                        { return null; }

                        public MyOther Child // MyOther pointer expected
                        { get; set; }

                        public MyOther this[int i] // MyOther pointer expected
                        {
                            get { return null; }
                        }
                    }
                }
                """;

            var result = await TestCodeGenerator.GenerateAsync([src], ct: CancellationToken);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/myapp/usesother.h", out var hpp));

            Assert.MatchesRegex(new Regex(
                @"Q_INVOKABLE\s+void\s+acceptOther\s*\(\s*MyApp::MyOther\s+\*\s*o\s*\)\s+const\s*;",
                    RegexOptions.Singleline),
                hpp, "param must be qobject-derived type as 'MyApp::MyOther *'");

            Assert.MatchesRegex(new Regex(
                @"Q_INVOKABLE\s+MyApp::MyOther\s*\*\s*make\s*\(\s*\)\s*const;",
                    RegexOptions.Singleline),
                hpp, "Return value must be qobject-derived type as 'MyApp::MyOther *'");

            Assert.MatchesRegex(new Regex(@"Q_PROPERTY\s*\(\s*MyApp::MyOther\s*\*\s*child\b",
                RegexOptions.Singleline), hpp, " Q_PROPERTY must declare 'MyApp::MyOther * Child'.");

            Assert.MatchesRegex(new Regex(@"MyApp::MyOther\s*\*\s*child\s*\(\s*\)\s*const;",
                RegexOptions.Singleline),
                hpp, "Property-get must be qobject-derived type as 'MyApp::MyOther *'");


            Assert.MatchesRegex(new Regex(@"void\s+setChild\s*\(\s*MyApp::MyOther\s*\*\s*value\s*\);",
                RegexOptions.Singleline),
                hpp, "Property-set must be qobject-derived type as 'MyApp::MyOther *'");

            Assert.MatchesRegex(new Regex(
                @"Q_INVOKABLE\s+MyApp::MyOther\s*\*\s*item\s*\(\s*qint32\s+i\s*\)\s*const;",
                    RegexOptions.Singleline),
                hpp, "Indexer-get must be qobject-derived type as 'MyApp::MyOther *'");
        }

        [TestMethod]
        public async Task ArgTrait_ValueType_MethodParams_ByValue()
        {
            const string src = """
                namespace MyApp {
                    public class Foo
                    {
                        public void takeValues(int a, double b)
                        {}
                    }
                }
                """;

            var result = await TestCodeGenerator.GenerateAsync([src], ct: CancellationToken);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/myapp/foo.h", out var hpp));

            Assert.DoesNotMatchRegex(new Regex(@"void\s+takeValues\s*\([^)]*qint32\s*\*",
                RegexOptions.Singleline), hpp, "Unexpected qint32* generated.");
            Assert.DoesNotMatchRegex(new Regex(@"void\s+takeValues\s*\([^)]*double\s*\*",
                RegexOptions.Singleline), hpp, "Unexpected double* generated.");
        }

        [TestMethod]
        public async Task ArgTrait_ValueTypes_InCtor_Indexer_Property_ByValue()
        {
            const string src = """
                namespace MyApp {
                    public class Bar {
                        public int Count { get; set; }
                        public Bar(int n, double x) {}
                        public object this[int i, double d] { get { return null; } }
                    }
                }
                """;

            var result = await TestCodeGenerator.GenerateAsync([src], ct: CancellationToken);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/myapp/bar.h", out var hpp));

            Assert.MatchesRegex(new Regex(@"Bar\s*\(\s*qint32\s+n\s*,\s*double\s+x\s*\)\s*;",
                RegexOptions.Singleline), hpp, "Ctor should be qint32 and double by-value.");

            Assert.MatchesRegex(new Regex(@"Q_INVOKABLE\s+QVariant\s+item\s*\(\s*qint32"
                + @"\s+i\s*,\s*double\s+d\s*\)\s*const;", RegexOptions.Singleline), hpp,
                "Indexer-get expected qint32/double by-value and return as QVariant (object).");

            Assert.MatchesRegex(new Regex(@"Q_PROPERTY\s*\(\s*qint32\s+count\b",
                RegexOptions.Singleline), hpp, "Q_PROPERTY should lead to qint32 for int property.");
            Assert.MatchesRegex(new Regex(@"qint32\s+count\s*\(\)\s*const;", RegexOptions.Singleline),
                hpp, "Get should be qint32 by-value.");
            Assert.MatchesRegex(new Regex(@"void\s+setCount\s*\(\s*qint32\s+value\s*\);",
                RegexOptions.Singleline), hpp, "Set should accept qint32 by-value.");
        }

        [TestMethod]
        public async Task No_Star_Before_ConvertFromVariant()
        {
            const string src = """
                namespace MyApp {
                    public class Foo
                    {
                        public void Echo(object x)
                        {}
                        public Foo(object x)
                        {}
                        public object item(object a) { return null; }
                        public object this[object a] { get { return null; } }
                        public object Data { get; set; }
                    }
                }
                """;

            var result = await TestCodeGenerator.GenerateAsync([src], ct: CancellationToken);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/myapp/foo.h", out var hpp));

            Assert.DoesNotMatchRegex(new Regex(@"\*\s*Convert::fromVariant\s*\(",
                RegexOptions.Singleline), hpp, "Unexpected: *Convert::fromVariant(...)");

            Assert.MatchesRegex(new Regex(@"echo\s*\(\s*QVariant\s+x\s*\)\s*const;",
                RegexOptions.Singleline), hpp, "object-Arg should become QVariant in Q_INVOKABLE.");
            Assert.MatchesRegex(new Regex(@"Q_PROPERTY\s*\(\s*QVariant\s+data\b",
                RegexOptions.Singleline), hpp, "object-Property should be exposed as QVariant.");
        }

        [TestMethod]
        public async Task ArgTrait_ValueType_MethodReturn_ByValue()
        {
            const string src = """
               namespace MyApp
               {
                   public class Foo
                   {
                       public int answer() => 42;
                       public double pi() => 3.14;
                   }
               }
               """;

            var result = await TestCodeGenerator.GenerateAsync([src], ct: CancellationToken);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/myapp/foo.h", out var hpp));

            Assert.DoesNotContain("qint32 *answer()", hpp);
            Assert.Contains("Q_INVOKABLE qint32 answer() const;", hpp);

            Assert.DoesNotContain("double *pi()", hpp);
            Assert.Contains("Q_INVOKABLE double pi() const;", hpp);
        }

        [TestMethod]
        public async Task ArgTrait_ValueType_FieldWrapper_ByValue()
        {
            const string src = """
               namespace MyApp
               {
                   public class Foo
                   {
                       public int Count;
                       public double Rate;
                   }
               }
            """;

            var result = await TestCodeGenerator.GenerateAsync([src], ct: CancellationToken);
            Assert.IsTrue(result.Sink.Files.TryGetValue("source/hpp/myapp/foo.h", out var hpp));

            Assert.DoesNotContain("qint32 *count()", hpp);
            Assert.Contains("qint32 count() const;", hpp);
            Assert.Contains("Q_PROPERTY(qint32 count", hpp);
            Assert.Contains("void setCount(qint32 value);", hpp);

            Assert.DoesNotContain("double *rate()", hpp);
            Assert.Contains("double rate() const;", hpp);
            Assert.Contains("Q_PROPERTY(double rate", hpp);
            Assert.Contains("void setRate(double value);", hpp);
        }
    }
}
