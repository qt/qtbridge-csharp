/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.DotNet.Generator
{
    using Qt;
    using Support;

    [TestClass]
    public class Test_FilterAttributes
    {
        private static readonly Assembly AdapterAssembly = typeof(IncludeAttribute).Assembly;

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Exclude_ExactType_RemovesTypeAndEdges()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.B))]
                namespace Test
                {
                    public class A
                    {
                        public B DependsB { get; set; }
                    }
                    public class B
                    {
                        public int X { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var typeA = graph.Keys.Single(t => t.FullName == "Test.A");
            Assert.IsTrue(graph.ContainsKey(typeA), "A should be present");

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.B"),
                "B must be excluded by [assembly:Exclude(typeof(B))]");

            Assert.IsFalse(graph.Connected(typeA).Any(t => t.FullName == "Test.B"),
                "Edge A -> B must not exist when B is excluded");

            Assert.IsFalse(graph[typeA].OfType<PropertyInfo>().Any(p => p.Name == "DependsB"),
                "Property A.DependsB should not be added because its type is excluded");
        }

        [TestMethod]
        public async Task Exclude_Inherited_ByType_RemovesDerivedTypes()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.Base), Inherited = true)]
                namespace Test
                {
                    public class Base
                    {}
                    public class Derived
                      : Base
                    {}
                    public class Holder
                    {
                      public Derived Value { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var holder = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsTrue(graph.ContainsKey(holder), "Holder should be present");

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Derived"),
                "Derived must be excluded because Base is excluded with Inherited = true");

            Assert.IsFalse(graph[holder].OfType<PropertyInfo>().Any(p => p.Name == "Value"),
                "Holder.Value property should be skipped because its type is excluded");
        }

        [TestMethod]
        public async Task Exclude_Inherited_ByStringName_ShouldRemoveDerivedTypes()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType("Test.Base", Inherited = true)]
                namespace Test
                {
                    public class Base
                    {}
                    public class Derived
                        : Base
                    {}
                    public class Holder
                    {
                        public Derived Value { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var holder = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsTrue(graph.ContainsKey(holder), "Holder should be present");

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Derived"),
                "Derived must be excluded because Base is excluded with Inherited = true");

            Assert.IsFalse(graph[holder].OfType<PropertyInfo>().Any(p => p.Name == "Value"),
                "Holder.Value property should be skipped because its type is excluded");
        }

        [TestMethod]
        public async Task Ignore_OnType_RemovesTypeEverywhere()
        {
            const string src = """
                using Qt;
                namespace Test
                {
                    [Qt.Ignore]
                    public class Ghost
                    {
                        public int X { get; set; }
                    }
                    public class UsesGhost
                    {
                        public Ghost G { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Ghost"),
                "[Ignore] on type should exclude it from the graph");

            var usesGhost = graph.Keys.Single(t => t.FullName == "Test.UsesGhost");
            Assert.IsTrue(graph.ContainsKey(usesGhost), "UsesGhost should be present");

            Assert.IsFalse(graph[usesGhost].OfType<PropertyInfo>().Any(p => p.Name == "G"),
                "Property referencing an ignored type must be skipped");
        }

        [TestMethod]
        public async Task Ignore_OnMember_RemovesOnlyThatMember()
        {
            const string src = """
                using Qt;
                namespace Test
                {
                    public class Data
                    {
                        [Qt.Ignore]
                        public int SkipMe { get; set; }
                        public int KeepMe { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var data = graph.Keys.Single(t => t.FullName == "Test.Data");
            var members = graph[data].OfType<PropertyInfo>().ToArray();

            Assert.IsFalse(members.Any(p => p.Name == "SkipMe"), "[Ignore] should drop the member");
            Assert.IsTrue(members.Any(p => p.Name == "KeepMe"), "Other members should remain");
        }

        [TestMethod]
        public async Task Include_OnType_Overrides_AssemblyExclude()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.X))]
                namespace Test
                {
                    [Qt.Include]
                    public class X
                    {
                        public int N { get; set; }
                    }
                    public class Holder
                    {
                        public X Value { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var x = graph.Keys.Single(t => t.FullName == "Test.X");
            Assert.IsTrue(graph.ContainsKey(x),
                "[Include] on X should override assembly-level exclude");

            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsTrue(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "Value"),
                "Holder.Value should be present since X is not excluded anymore");
        }

        [TestMethod]
        public async Task Exclude_IEnumerable_Inherited_Excludes_Implementations_And_Collections()
        {
            const string src = """
                using Qt;
                using System.Collections;
                using System.Collections.Generic;

                [assembly: Qt.IgnoreType(typeof(IEnumerable<>), Inherited = true)]
                namespace Test
                {
                    public class ImplementsSeq : IEnumerable<int>
                    {
                        public IEnumerator<int> GetEnumerator()
                            => System.Linq.Enumerable.Empty<int>().GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }

                    public class Holder
                    {
                        public ImplementsSeq A { get; set; } // own implementation
                        public List<int> B { get; set; } // BCL-Collection
                        public int[] C { get; set; } // Array (impl. IEnumerable<int>)
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src],
                sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.ImplementsSeq"),
                "ImplementsSeq should be excluded by [Exclude(typeof(IEnumerable<>), Inherited=true)].");

            var holder = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsTrue(graph.ContainsKey(holder), "Holder should be present");

            var props = graph[holder].OfType<PropertyInfo>().Select(p => p.Name).ToArray();
            CollectionAssert.DoesNotContain(props, "A",
                "Holder.A property must be excluded (user-provided IEnumerable implementation).");
            CollectionAssert.DoesNotContain(props, "B",
                "Holder.B property (List<int>) must be excluded (implements IEnumerable<int>).");
            CollectionAssert.DoesNotContain(props, "C",
                "Holder.C property (int[]) must be excluded (Array implements IEnumerable<int>).");
        }

        [TestMethod]
        public async Task Exclude_IEnumerable_AssemblyQualifiedName_Excludes_Implementations()
        {
            var assemblyName = typeof(object).Assembly.GetName().Name;
            var excludedTypeString = $"System.Collections.Generic.IEnumerable`1,{assemblyName}";

            var src = $$"""
                using Qt;
                using System.Collections;
                using System.Collections.Generic;
                [assembly: Qt.IgnoreType("{{excludedTypeString}}", Inherited = true)]
                namespace Test
                {
                    public class ImplementsSeq : IEnumerable<int>
                    {
                        public IEnumerator<int> GetEnumerator()
                            => System.Linq.Enumerable.Empty<int>().GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }
                    public class Holder
                    {
                        public ImplementsSeq A { get; set; }
                        public List<int> B { get; set; }
                        public int[] C { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src],
                sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.ImplementsSeq"),
                "Type 'Test.ImplementsSeq' should be excluded by [assembly: Qt.IgnoreType(\"System"
                + $".Collections.Generic.IEnumerable`1,{assemblyName}\", Inherited = true)].");

            var holder = graph.Keys.SingleOrDefault(t => t.FullName == "Test.Holder");
            Assert.IsNotNull(holder, "Type 'Test.Holder' must be present in the graph.");

            var props = graph[holder]
                .OfType<PropertyInfo>()
                .Select(p => p.Name)
                .ToArray();

            CollectionAssert.DoesNotContain(props, "A",
                "Holder.A property must be excluded (user-provided IEnumerable implementation.");
            CollectionAssert.DoesNotContain(props, "B",
                "Holder.B property (List<int>) must be excluded (implements IEnumerable<int>).");
            CollectionAssert.DoesNotContain(props, "C",
                "Holder.C property (int[]) must be excluded (Array implements IEnumerable<int>).");
        }

        [TestMethod]
        public async Task Exclude_Interface_With_Inherited_Excludes_Implementors()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.IThing), Inherited = true)]
                namespace Test
                {
                    public interface IThing
                    {}
                    public class Impl : IThing
                    {}
                    public class Holder
                    {
                        public Impl V { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Impl"));
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "V"));
        }

        [TestMethod]

        public async Task Exclude_OpenGenericBase_With_Inherited_Excludes_ClosedDerived()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.Base<>), Inherited = true)]
                namespace Test
                {
                    public class Base<T>
                    {}
                    public class Derived
                        : Base<int>
                    {}
                    public class Holder
                    {
                        public Derived V { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Derived"));
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "V"));
        }

        [TestMethod]
        public async Task Member_Ignore_Is_Not_Inherited_To_Overrides()
        {
            const string src = """
                using Qt;
                namespace Test
                {
                    public class B
                    {
                        [Qt.Ignore]
                        public virtual int P { get; set; }
                    }
                    public class D : B
                    {
                        public override int P { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var d = graph.Keys.Single(t => t.FullName == "Test.D");
            // Expectation: Ignore is not [Inherited]; Override in D remains visible
            Assert.IsTrue(graph[d].OfType<PropertyInfo>().Any(p => p.Name == "P"));
        }

        [TestMethod]
        public async Task BaseMethod_Ignored_DerivedOverride_Remains_Visible()
        {
            const string src = """
                using Qt;
                namespace Test
                {
                    public class B
                    {
                        [Qt.Ignore]
                        public virtual int M() => 0;
                    }
                    public class D : B
                    {
                        public override int M() => 1;
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var d = graph.Keys.Single(t => t.FullName == "Test.D");
            Assert.IsTrue(graph[d].OfType<MethodInfo>().Any(m => m.Name == "M"),
                "Override 'D.M' remains visible: Member-[Ignore] is not inheritable.");
        }

        [TestMethod]
        public async Task IncludeType_Overrides_AssemblyExclude()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.K))]
                namespace Test
                {
                    [Qt.Include]
                    public class K
                    {
                        public int P { get; set; }
                    }

                    public class Holder
                    {
                        public K A { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsTrue(graph.Keys.Any(t => t.FullName == "Test.K"),
                "Type-level [Include] should restore K despite assembly-level exclude.");
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsTrue(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "A"),
                "Holder.A must be present since K is included.");
        }

        [TestMethod]
        public async Task TypeExcluded_RemovesTypeAndReferencingMembers()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.L))]
                namespace Test
                {
                    public class L
                    {
                        public int M { get; set; }
                    }

                    public class Holder
                    {
                        public L B { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.L"),
                "L must remain excluded by [assembly: IgnoreType].");
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "B"),
                "Holder.B must be excluded, since L is excluded.");
        }

        [TestMethod]
        public async Task MemberInclude_IsRejected_ByCompiler()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.L))]
                namespace Test
                {
                    public class L
                    {
                        [Qt.Include] // not valid on members anymore
                        public int M { get; set; }
                    }
                }
            """;

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
                await TestCodeGenerator.GenerateAsync([src],
                    sourceRefs: [AdapterAssembly],
                    ct: TestContext.CancellationTokenSource.Token
                )
            );
        }

        [TestMethod]
        public async Task Delegate_Type_Can_Be_Excluded()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.MyDel))]
                namespace Test
                {
                    public delegate void MyDel(int x);
                    public class Uses { public MyDel D; }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.MyDel"),
                "Test.MyDel should be excluded by [Exclude(typeof(Test.MyDel))].");

            var uses = graph.Keys.Single(t => t.FullName == "Test.Uses");
            Assert.IsFalse(graph[uses].OfType<FieldInfo>().Any(f => f.Name == "D"),
                "Field 'D' must be excluded when the delegate type is excluded.");
        }

        [TestMethod]
        public async Task Delegate_Param_And_Return_Removed_When_PayloadType_Excluded()
        {
            const string src = """
                using Qt; using System;
                [assembly: Qt.IgnoreType(typeof(Test.Ex))]
                namespace Test
                {
                    public class Ex
                    {}
                    public class Svc
                    {
                        public void WithDelegateParam(Action<Ex> cb)
                        {}
                        public Func<Ex> Make() => null;
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Ex"),
                "Excluded type 'Test.Ex' must not be present in the graph.");

            var svc = graph.Keys.Single(t => t.FullName == "Test.Svc");

            Assert.IsFalse(graph[svc].OfType<MethodInfo>().Any(m => m.Name == "WithDelegateParam"),
                "Method 'Svc.WithDelegateParam(Action<Ex>)' must be removed because the delegate "
                    + "payload type is excluded.");

            Assert.IsFalse(graph[svc].OfType<MethodInfo>().Any(m => m.Name == "Make"),
                "Method 'Svc.Make(): Func<Ex>' must be removed because the delegate payload type "
                    + "in the return is excluded.");
        }

        [TestMethod]
        public async Task Exclude_MultiArgs_Removes_Types_And_Edges()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.A), typeof(Test.B))]
                namespace Test
                {
                    public class A
                    {}
                    public class B
                    {}
                    public class Holder
                    {
                        public A X { get; set; }
                        public B Y { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.A"),
                "Type 'Test.A' must be excluded by multi-argument [assembly: Qt.IgnoreType].");
            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.B"),
                "Type 'Test.B' must be excluded by multi-argument [assembly: Qt.IgnoreType].");

            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            var props = graph[h].OfType<PropertyInfo>().Select(p => p.Name).ToArray();
            CollectionAssert.DoesNotContain(props, "X", "Holder.X must be excluded (A excluded).");
            CollectionAssert.DoesNotContain(props, "Y", "Holder.Y must be excluded (B excluded).");

            Assert.IsFalse(graph.Connected(h).Any(t => t.FullName is "Test.A" or "Test.B"),
                "Edges from Holder to excluded types must be absent.");
        }

        [TestMethod]
        public async Task Exclude_Core_StringType_Removes_String_Property()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType("System.String")]
                namespace Test
                {
                    public class Holder
                    {
                        public string S { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            var props = graph[h].OfType<PropertyInfo>().Select(p => p.Name).ToArray();
            CollectionAssert.DoesNotContain(props, "S",
                "Holder.S (string) must be excluded by [assembly: Qt.IgnoreType(\"System.String\")].");
        }

        [TestMethod]
        public async Task Type_Include_Overrides_Type_Ignore()
        {
            const string src = """
                using Qt;
                namespace Test
                {
                    [Qt.Ignore]
                    [Qt.Include]
                    public class T
                    {
                        public int P { get; set; }
                    }
                    public class Holder
                    {
                        public T Val { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsTrue(graph.Keys.Any(t => t.FullName == "Test.T"),
                "Type-level [Include] must override [Ignore] and keep 'Test.T'.");

            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            var props = graph[h].OfType<PropertyInfo>().Select(p => p.Name).ToArray();
            CollectionAssert.Contains(props, "Val",
                "Holder.Val must remain present since type-level Include wins.");
        }

        [TestMethod]
        public async Task ExcludedType_Removes_MethodParams_And_Events()
        {
            const string src = """
                using Qt;
                using System;
                [assembly: Qt.IgnoreType(typeof(Test.Ex))]
                namespace Test
                {
                    public class Ex
                    {}

                    public class Svc
                    {
                        public void M(Ex x) {}
                        public event Action<Ex> Ev;
                        public Ex Returner() => null;
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Ex"),
                "Excluded type 'Test.Ex' must not be present in the graph.");

            var svc = graph.Keys.Single(t => t.FullName == "Test.Svc");
            Assert.IsFalse(graph[svc].OfType<MethodInfo>().Any(m => m.Name == "M"),
                "Method 'Svc.M(Ex)' must be excluded because its parameter type is excluded.");
            Assert.IsFalse(graph[svc].OfType<EventInfo>().Any(e => e.Name == "Ev"),
                "Event 'Svc.Ev' must be excluded because its delegate generic argument is excluded.");
            Assert.IsFalse(graph[svc].OfType<MethodInfo>().Any(m => m.Name == "Returner"),
                "Method 'Svc.Returner' must be excluded because its return type is excluded.");
        }

        [TestMethod]
        public async Task Exclude_Interface_Inherited_Removes_MultiImplements()
        {
            const string src = """
               using Qt;
               [assembly: Qt.IgnoreType(typeof(Test.I1), Inherited = true)]
               namespace Test
               {
                   public interface I1
                   {}
                   public interface I2
                   {}
                   public class D : I1, I2
                   {}
                   public class Holder
                   {
                       public D V { get; set; }
                   }
               }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.D"),
                "Implementer of excluded interface I1 must be removed.");
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "V"),
                "Holder.V must be removed because its type implements excluded I1.");
        }

        [TestMethod]
        public async Task Exclude_BaseClass_Inherited_Removes_Derived_With_ExtraInterfaces()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.Base), Inherited = true)]
                namespace Test
                {
                    public class Base
                    {}
                    public interface IMark
                    {}
                    public class D : Base, IMark
                    {}
                    public class Holder
                    {
                        public D V { get; set; }
                    }
               }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.D"),
                "Derived class must be removed when base class is excluded with Inherited=true.");
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "V"),
                "Holder.V must be removed because D is excluded.");
        }

        [TestMethod]
        public async Task Exclude_Interface_With_Default_Impl_Inherited_Removes_Implementer()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.IDef), Inherited = true)]
                namespace Test
                {
                    public interface IDef
                    {
                        void M() { } // default implementation (C# 8+)
                    }
                    public class Impl : IDef
                    {} // relies on default M()
                    public class Holder
                    {
                        public Impl V { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Impl"),
                "Implementer must be removed even if interface has default methods.");
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "V"),
                "Holder.V must be removed because Impl implements excluded IDef.");
        }

        [TestMethod]
        public async Task Exclude_Outer_Cascades_To_Inner()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.Outer))]
                namespace Test
                {
                    public class Outer
                    {
                        public class Inner {}
                    }
                    public class Holder
                    {
                        public Outer.Inner X { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t =>
                t.FullName == "Test.Outer"), "Outer must be excluded.");
            Assert.IsFalse(graph.Keys.Any(t =>
                t.FullName == "Test.Outer+Inner"), "Inner must be excluded");

            var holder = graph.Keys.Single(t => t.FullName == "Test.Holder");
            var props = graph[holder].OfType<PropertyInfo>().Select(p => p.Name).ToArray();

            CollectionAssert.DoesNotContain(props, "X",
                "With cascade policy, Holder.X must be removed because Outer.Inner is excluded.");
        }

        [TestMethod]
        public async Task Include_Inner_Overrides_Outer_Exclude()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.Outer))]
                namespace Test
                {
                    public class Outer
                    {
                        [Qt.Include]
                        public class Inner {}
                    }
                    public class Holder
                    {
                        public Outer.Inner X { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var g = res.Graph;

            Assert.IsFalse(g.Keys.Any(t => t.FullName == "Test.Outer"),
                "Outer must be excluded by assembly-level rule.");
            Assert.IsTrue(g.Keys.Any(t => t.FullName == "Test.Outer+Inner"),
                "Inner must remain because it has a type-level [Include].");

            var h = g.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsTrue(g[h].OfType<PropertyInfo>().Any(p => p.Name == "X"),
                "Holder.X must be present since Inner is included.");
        }

        [TestMethod]
        public async Task Exclude_NestedType_Removes_References()
        {
            const string src = """
                using Qt;
                [assembly: Qt.IgnoreType(typeof(Test.Outer.Inner))]
                namespace Test
                {
                    public class Outer
                    {
                        public class Inner {}
                    }
                    public class Holder
                    {
                        public Outer.Inner X { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            Assert.IsFalse(graph.Keys.Any(t => t.FullName == "Test.Outer+Inner"),
                "Explicit exclusion of the nested type must remove it.");
            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            Assert.IsFalse(graph[h].OfType<PropertyInfo>().Any(p => p.Name == "X"),
                "Holder.X must be removed because Outer.Inner is excluded.");
        }

        [TestMethod]
        public async Task Exclude_OpenGeneric_List_Removes_Closed_Generic_Properties()
        {
            const string src = """
                using Qt;
                using System.Collections.Generic;
                [assembly: Qt.IgnoreType(typeof(List<>))]
                namespace Test
                {
                    public class Holder
                    {
                        public List<int> L { get; set; }
                    }
                }
            """;

            var res = await TestCodeGenerator.GenerateAsync([src], sourceRefs: [AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);
            var graph = res.Graph;

            var h = graph.Keys.Single(t => t.FullName == "Test.Holder");
            var props = graph[h].OfType<PropertyInfo>().Select(p => p.Name).ToArray();
            CollectionAssert.DoesNotContain(props, "L",
                "Holder.L (List<int>) must be removed when List<> is excluded.");
        }


    }
}
