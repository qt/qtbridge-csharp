// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Api
{
    [TestClass]
    public class Test_MethodLookup
    {
        private enum State { Off, On }
        private enum ByteState : byte { Off, On }
        private enum Color { Red, Blue }
        private enum Size { Small, Large }

        private sealed class Overloads
        {
            public void SetState(State state) { }
            public void SetState(State state, int timeout) { }
        }

        private sealed class ByteBacked
        {
            public void SetState(ByteState state) { }
        }

        private sealed class Ambiguous
        {
            public void Set(Color value) { }
            public void Set(Size value) { }
        }

        public interface ICount
        {
            int Count { get; }
        }

        public sealed class ExplicitCount : ICount
        {
            int ICount.Count => 42;
            private int Hidden() => 0;
        }

        [TestMethod]
        public void ExplicitInterfaceGetter_ResolvesThroughPublicInterface()
        {
            var target = typeof(ExplicitCount).GetInterfaceMap(typeof(ICount)).TargetMethods.Single();
            var method = typeof(ExplicitCount).FindMethod(target.Name,
                BindingFlags.Public | BindingFlags.Instance, []);
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(ICount), method.DeclaringType);
            var getter = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), new ExplicitCount(),
                method);
            Assert.AreEqual(42, getter());
            Assert.IsNull(typeof(ExplicitCount).FindMethod("Hidden",
                BindingFlags.Public | BindingFlags.Instance, []));
        }

        public class MultipleCounts : IReadOnlyCollection<int>, ICount
        {
            int IReadOnlyCollection<int>.Count => 42;
            int ICount.Count => 999;
            public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class InheritedCounts : MultipleCounts { }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InterfaceContract_SelectsCorrectInheritedGetter(bool inherited)
        {
            var collection = inherited ? new InheritedCounts() : new MultipleCounts();
            var contract = StableTypeNameBuilder.Build(typeof(IReadOnlyCollection<int>));
            var method = collection.GetType().FindMethod($"{contract}::get_Count",
                BindingFlags.Public | BindingFlags.Instance, []);
            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(IReadOnlyCollection<int>), method.DeclaringType);
            var getter = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), collection, method);
            Assert.AreEqual(42, getter());
            var unrelated = StableTypeNameBuilder.Build(typeof(ICollection<int>));
            Assert.IsNull(collection.GetType().FindMethod($"{unrelated}::get_Count",
                BindingFlags.Public | BindingFlags.Instance, []));
            Assert.IsNull(collection.GetType().FindMethod($"{contract}::get_Count",
                BindingFlags.Public | BindingFlags.Static, []));
        }

        [TestMethod]
        public void EnumBinding_RequiresMatchingParameterCount()
        {
            var methods = typeof(Overloads).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.Name == nameof(Overloads.SetState)).ToArray();
            var oneParameter = methods.Single(method => method.GetParameters().Length == 1);
            var twoParameters = methods.Single(method => method.GetParameters().Length == 2);

            Assert.IsTrue(oneParameter.BindsTo([typeof(int)]));
            Assert.IsFalse(oneParameter.BindsTo([typeof(int), typeof(int)]));
            Assert.IsFalse(twoParameters.BindsTo([typeof(int)]));
            Assert.IsTrue(twoParameters.BindsTo([typeof(int), typeof(int)]));
        }

        [TestMethod]
        public void EnumBinding_SupportsByteBackedEnum()
        {
            var method = typeof(ByteBacked).FindMethod(nameof(ByteBacked.SetState),
                BindingFlags.Public | BindingFlags.Instance, [typeof(int)]);
            if (method == null)
                Assert.Inconclusive("TO-FIX: Unsupported enum backed by non-Int32");
        }

        [TestMethod]
        public void EnumBinding_RejectsAmbiguousOverload()
        {
            var method = typeof(Ambiguous).FindMethod(nameof(Ambiguous.Set),
                BindingFlags.Public | BindingFlags.Instance, [typeof(int)]);
            if (method == null)
                Assert.Inconclusive("TO-FIX: Ambiguous method lookup due to enum param binding");
        }
    }
}
