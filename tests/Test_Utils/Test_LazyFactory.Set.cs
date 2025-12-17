/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.Bridge.Utils;

namespace Test_Utils
{
    public partial class Test_LazyFactory
    {
        // 1. Simple Set stores value for later Get
        [TestMethod]
        public void Set_InstanceProperty_StoresValue_ForSubsequentGet()
        {
            var factory = new LazyFactory();
            var foo = new Dummy();

            factory.Set(() => foo.Prop, 99);
            Assert.AreEqual(99, factory.Get(() => foo.Prop));
        }

        // 2. Set overwrites any previous lazy or explicit value
        [TestMethod]
        public void Set_OverridesPriorValue_LazyOrExplicit()
        {
            var factory = new LazyFactory();
            var foo = new Dummy();

            // first via initFunc
            Assert.AreEqual(1, factory.Get(() => foo.Prop, () => 1));
            // now override
            factory.Set(() => foo.Prop, 42);
            Assert.AreEqual(42, factory.Get(() => foo.Prop));
        }

        // 3. Distinct owners remain isolated after Set
        [TestMethod]
        public void Set_DistinctOwners_DoNotAffectEachOther()
        {
            var factory = new LazyFactory();
            var a = new Dummy();
            var b = new Dummy();

            factory.Set(() => a.Prop, 5);
            factory.Set(() => b.Prop, 10);

            Assert.AreEqual(5, factory.Get(() => a.Prop));
            Assert.AreEqual(10, factory.Get(() => b.Prop));
        }

        // 4. Set raises PropertyChanged on the factory with owner as sender
        [TestMethod]
        public void Set_RaisesPropertyChanged_OnFactory_WithOwnerAsSender()
        {
            var factory = new LazyFactory();
            var foo = new NotifyingDummy();
            var fired = false;

            factory.PropertyChanged += (sender, args) =>
            {
                fired = true;
                // the sender should be the owner instance
                Assert.AreSame(foo, sender);
                Assert.AreEqual(nameof(NotifyingDummy.Prop), args.PropertyName);
            };

            factory.Set(() => foo.Prop, 123);

            Assert.IsTrue(fired);
        }

        // 5. Set on static property still stores & Get retrieves
        [TestMethod]
        public void Set_StaticProperty_StoresAndRetrievesValue()
        {
            var factory = new LazyFactory();

            factory.Set(() => AppSettings.Threshold, 0.9);
            Assert.AreEqual(0.9, factory.Get(() => AppSettings.Threshold));
        }

        // 6. Set does not raise PropertyChanged for static props
        [TestMethod]
        public void Set_StaticProperty_DoesNotRaisePropertyChanged()
        {
            var factory = new LazyFactory();
            var fired = false;

            factory.PropertyChanged += (_, _) => fired = true;

            factory.Set(() => AppSettings.IsFeatureEnabled, true);
            Assert.IsTrue(fired, "Static owner (Type) does not implement INotifyPropertyChanged, "
               + "though PropertyChanged needs to be raised anyway.");
        }

        // 7. Concurrent Set + Get: Set shall win, and subsequent Gets see latest
        [TestMethod]
        public void ConcurrentSetAndGet_UpdatesAtomically()
        {
            var factory = new LazyFactory();
            var foo = new Dummy();

            // Start a Set in background, sleep then assign
            var setter = Task.Run(() =>
            {
                Thread.Sleep(50);
                factory.Set(() => foo.Prop, 77);
            });

            // Multiple Gets will spin until they see the update:
            int observed;
            do {
                observed = factory.Get(() => foo.Prop);
            } while (observed == 0);

            setter.Wait();
            Assert.AreEqual(77, observed);
        }

        // 8. Setting to default(T) shall write into cache
        [TestMethod]
        public void Set_ToDefaultValue_IsStoredAndReturned()
        {
            var factory = new LazyFactory();
            var foo = new Dummy { Prop = 5 };

            // override back to default( int ) == 0
            factory.Set(() => foo.Prop, 0);
            Assert.AreEqual(0, factory.Get(() => foo.Prop));
        }

        // 9. Set after exception‑throwing initFunc still persists
        [TestMethod]
        public void Set_AfterFailedInit_PersistsNewValue()
        {
            var factory = new LazyFactory();
            var foo = new Dummy();
            Func<int> badInit = () => throw new InvalidOperationException();

            // fail first
            Assert.ThrowsExactly<InvalidOperationException>(() => factory.Get(() => foo.Prop, badInit));

            // now Set explicitly
            factory.Set(() => foo.Prop, 55);
            Assert.AreEqual(55, factory.Get(() => foo.Prop));
        }
    }
}
