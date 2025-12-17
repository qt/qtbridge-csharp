/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Concurrent;
using System.Reflection;

namespace Test_Utils
{
    using Qt.Bridge.Utils;

    [TestClass]
    public partial class Test_LazyFactory
    {
        // Verify that when one owner initializes the property with an initFunc, a second owner
        // without an initFunc does not erroneously reuse the cached value.
        [TestMethod]
        public void SecondOwnerWithoutInitFunc_ShouldNotShareValue()
        {
            var factory = new LazyFactory();
            var owner1 = new Dummy();
            var owner2 = new Dummy();
            var initCalls = 0;

            // First owner initializes via initFunc
            var value1 = factory.Get(() => owner1.Prop, () => Interlocked.Increment(ref initCalls));
            // Second owner without initFunc should not pick up the same stored value
            var value2 = factory.Get(() => owner2.Prop);

            Assert.AreEqual(1, initCalls, "InitFunc should only have been called once.");
            Assert.AreNotEqual(value1, value2, "Expected second owner to not share the initialized "
                + "value.");
        }

        // Ensure that concurrent 'Get' calls on the same owner and property only invoke the
        // initializer once, preventing race conditions.
        [TestMethod]
        public void ConcurrentSameOwner_OnlyOneInitCall()
        {
            var factory   = new LazyFactory();
            var owner     = new Dummy();
            var callCount = 0;
            var initFunc = () => Interlocked.Increment(ref callCount);

            // Kick off two Gets in parallel
            var t1 = Task.Run(() => factory.Get(() => owner.Prop, initFunc));
            var t2 = Task.Run(() => factory.Get(() => owner.Prop, initFunc));

            // Wait for both to finish
            Task.WaitAll(t1, t2);

            // Exactly one initializer invocation:
            Assert.AreEqual(1, callCount);

            // And both results are identical (i.e. the cached value):
            Assert.AreEqual(t1.Result, t2.Result);
        }

        // Verify that when the initializer throws, each 'Get' call invokes the initializer
        // again instead of caching the exception (no exception caching behavior).
        // TODO: Maybe introduce exception caching using Lazy with possible enum param to switch
        //       behavior.
        [TestMethod]
        public void InitFuncThrows_EachCallInvokesInit()
        {
            var factory = new LazyFactory();
            var owner = new Dummy();
            var throwCount = 0;
            Func<int> initFunc = () =>
            {
                _ = Interlocked.Increment(ref throwCount);
                throw new InvalidOperationException("Init failed");
            };

            // First call should invoke initFunc and throw
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => factory.Get(() => owner.Prop,
                initFunc));
            Assert.AreEqual(1, throwCount, "Initializer should have been called once");

            // Second call should invoke initFunc again and throw
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => factory.Get(() => owner.Prop,
                initFunc));
            Assert.AreEqual(2, throwCount, "Initializer should have been called on each Get");
        }

        // Memory risk:
        // We keep every (owner, PropertyInfo) pair forever. If we call 'Get' on hundreds or
        // thousands of different objects - or on many different properties - we'll accumulate
        // entries indefinitely.
        // TODO: Maybe use ConditionalWeakTable instead of ConcurrentDictionary. Another option
        //       is to implement a cleanup mechanism using a WeakKey and a cleanup GC collected
        //       owners method that runs periodically or on demand.
        [TestMethod]
        public void Cache_ShouldBeEmptyAfterOwnerGarbageCollection()
        {
            var factory = new LazyFactory();
            var foo = new Dummy();
            // Populate cache
            _ = factory.Get(() => foo.Prop, () => 123);

            // Reflect into private cache
            var objectProperties = typeof(LazyFactory)
                .GetProperty("ObjectsCache", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(objectProperties, "The objectProperties should not be null.");

            var cache = objectProperties.GetValue(factory)
                as ConcurrentDictionary<(object, PropertyInfo), object>;
            Assert.IsNotNull(cache, "The cache should not be null.");

            // Drop strong reference and force GC
            foo = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.Inconclusive("Cache cleanup not implemented; entries remain indefinitely.");
            // Expect cache to clean up stale entries
            Assert.IsEmpty(cache, "Expected cache to remove entries after owner is collected.");
        }

        // Behavioral quirk:
        // initFunc == null returns default(T).
        // If we call 'Get(() => Foo)' without an initializer, we'll always get default
        // (e.g., 0 or null). The factory shall not cache the default value.
        [TestMethod]
        public void WithoutInitFunc_GetReturnsDefault_AndAllowsLaterInit()
        {
            var factory = new LazyFactory();
            var foo = new Dummy();

            // 1) First call with no initFunc returns default(T) (e.g. 0 or null)
            Assert.AreEqual(0, factory.Get(() => foo.Prop));

            // 2) Still no caching of default—still default on a second no-init call
            Assert.AreEqual(0, factory.Get(() => foo.Prop));

            // 3) Now supply an initFunc; it runs once and caches the result
            Assert.AreEqual(42, factory.Get(() => foo.Prop, () => 42));

            // 4) Subsequent calls without an initFunc now return the cached 42
            Assert.AreEqual(42, factory.Get(() => foo.Prop));
        }

        // Behavioral quirk:
        // LazyFactory implements INotifyPropertyChanged, but in 'Set' we're raising the factory's
        // PropertyChanged event, passing the owner as the sender. Handlers wired on the owner's
        // own PropertyChanged won't fire, and handlers on the factory will get a "wrong" sender.
        [TestMethod]
        public void ShouldRaisePropertyChangedOnOwner()
        {
            var factory = new LazyFactory();
            var foo = new NotifyingDummy();
            var eventRaised = false;

            factory.PropertyChanged += (_, args) =>
            {
                eventRaised = true;
                Assert.AreEqual(nameof(NotifyingDummy.Prop), args.PropertyName);
            };

            factory.Set(() => foo.Prop, 100);

            Assert.AreEqual(100, factory.Get(() => foo.Prop));
            Assert.IsTrue(eventRaised, "Setting lazy property should raise PropertyChanged on the "
                + "factory instance passing the owner.");
        }

        // Make sure we are always raising the factory's event regardless of owner type.
        [TestMethod]
        public void ShouldRaisePropertyChangedOnOwner_WithoutINotifyPropertyChanged()
        {
            var factory = new LazyFactory();
            var eventRaised = false;

            factory.PropertyChanged += (_, args) =>
            {
                eventRaised = true;
                Assert.AreEqual(nameof(AppSettings.IsFeatureEnabled), args.PropertyName);
            };

            factory.Set(() => AppSettings.IsFeatureEnabled, true);

            Assert.IsTrue(factory.Get(() => AppSettings.IsFeatureEnabled));
            Assert.IsTrue(eventRaised);
        }
    }
}
