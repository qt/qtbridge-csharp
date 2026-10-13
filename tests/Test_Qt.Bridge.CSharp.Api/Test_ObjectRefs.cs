// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Api
{
    [TestClass]
    public class Test_ObjectRefs
    {
        public class Notifier : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public void Notify(string name) => PropertyChanged?.Invoke(this, new(name));
            public string Name() => "notifier";
        }

        // Objects that compare equal, but are distinct instances.
        public class Twin
        {
            public string Name() => "twin";
            public override bool Equals(object obj) => obj is Twin;
            public override int GetHashCode() => 0;
        }

        private static Parameter[] FuncParams(params Type[] types)
        {
            return [.. types.Select(t => new Parameter(t))];
        }

        private static int DelegateCount()
        {
            Adapter.Stats(out _, out var delegateCount, out _);
            return delegateCount;
        }

        private static int EventCount()
        {
            Adapter.Stats(out _, out _, out var eventCount);
            return eventCount;
        }

        private static nint ResolveName(nint objRefPtr)
        {
            return Adapter.ResolveInstanceMethod(objRefPtr, "Name", 1, FuncParams(typeof(string)));
        }

        [TestMethod]
        public void FreeObjectRef_KeepsDelegatesUntilLastRefIsFreed()
        {
            var obj = new Notifier();
            var firstRef = Adapter.GetRefPtrToObject(obj);
            var secondRef = Adapter.AddObjectRef(firstRef);
            var delegates = DelegateCount();

            Assert.AreNotEqual(nint.Zero, ResolveName(firstRef));
            Assert.AreEqual(delegates + 1, DelegateCount());

            Adapter.FreeObjectRef(firstRef);
            Assert.AreEqual(delegates + 1, DelegateCount());

            Adapter.FreeObjectRef(secondRef);
            Assert.AreEqual(delegates, DelegateCount());
        }

        [TestMethod]
        public void FreeObjectRef_KeepsDelegatesOfEqualObjects()
        {
            var firstRef = Adapter.GetRefPtrToObject(new Twin());
            var secondRef = Adapter.GetRefPtrToObject(new Twin());
            var delegates = DelegateCount();

            var firstName = ResolveName(firstRef);
            var secondName = ResolveName(secondRef);
            Assert.AreNotEqual(firstName, secondName);
            Assert.AreEqual(delegates + 2, DelegateCount());

            // The second delegate is bound to the second instance and must survive.
            Adapter.FreeObjectRef(firstRef);
            Assert.AreEqual(delegates + 1, DelegateCount());
            Assert.AreEqual(secondName, ResolveName(secondRef));

            Adapter.FreeObjectRef(secondRef);
            Assert.AreEqual(delegates, DelegateCount());
        }

        [TestMethod]
        public void FreeObjectRef_RemovesItsEventHandlers()
        {
            var obj = new Notifier();
            var objRef = Adapter.GetRefPtrToObject(obj);
            var otherRef = Adapter.AddObjectRef(objRef);
            var events = EventCount();
            var calls = 0;
            Adapter.Delegates.NativeEventHandler handler = (_, _, _, _) => ++calls;

            Adapter.AddEventHandler(objRef, nameof(Notifier.PropertyChanged), 1, handler);
            Adapter.AddEventHandler(otherRef, nameof(Notifier.PropertyChanged), 2, handler);
            Assert.AreEqual(events + 2, EventCount());
            obj.Notify("X");
            Assert.AreEqual(2, calls);

            Adapter.FreeObjectRef(objRef);
            Assert.AreEqual(events + 1, EventCount());
            obj.Notify("X");
            Assert.AreEqual(3, calls);

            Adapter.FreeObjectRef(otherRef);
            Assert.AreEqual(events, EventCount());
            obj.Notify("X");
            Assert.AreEqual(3, calls);
        }

        [TestMethod]
        public void RemoveAllEventHandlersByContext_RemovesOnlyThatContext()
        {
            var obj = new Notifier();
            var objRef = Adapter.GetRefPtrToObject(obj);
            var events = EventCount();
            Adapter.Delegates.NativeEventHandler handler = (_, _, _, _) => { };

            Adapter.AddEventHandler(objRef, nameof(Notifier.PropertyChanged), 10, handler);
            Adapter.AddEventHandler(objRef, nameof(Notifier.PropertyChanged), 11, handler);
            Assert.AreEqual(events + 2, EventCount());

            Adapter.RemoveAllEventHandlersByContext(10);
            Assert.AreEqual(events + 1, EventCount());

            Adapter.FreeObjectRef(objRef);
            Assert.AreEqual(events, EventCount());
        }

        [TestMethod]
        public void ResolveConstructor_ReusesTheDelegate()
        {
            var parameters = FuncParams(typeof(StringBuilder), typeof(string));
            var ctor = Adapter.ResolveConstructor(2, parameters);
            Assert.AreNotEqual(nint.Zero, ctor);

            var delegates = DelegateCount();
            Assert.AreEqual(1, delegates);

            Assert.AreEqual(ctor, Adapter.ResolveConstructor(2, parameters));
            Assert.AreEqual(delegates, DelegateCount());
        }

        public static string StaticName() => "static";

        [TestMethod]
        public void FreeObjectRef_KeepsStaticDelegatesOfTheType()
        {
            var staticName = Adapter.ResolveStaticMethod(
                typeof(Test_ObjectRefs).AssemblyQualifiedName, nameof(StaticName), 1,
                FuncParams(typeof(string)));
            var delegates = DelegateCount();

            // Native code caches static function pointers, so a short-lived reference to the
            // type must not take them along when it is freed.
            var typeRef = Adapter.GetRefPtrToObject(typeof(Test_ObjectRefs));
            Adapter.FreeObjectRef(typeRef);
            Assert.AreEqual(delegates, DelegateCount());
            Assert.AreEqual(staticName, Adapter.ResolveStaticMethod(
                typeof(Test_ObjectRefs).AssemblyQualifiedName, nameof(StaticName), 1,
                FuncParams(typeof(string))));

            Adapter.FreeTypeRef(typeof(Test_ObjectRefs).AssemblyQualifiedName);
            Assert.AreEqual(delegates - 1, DelegateCount());
        }
    }
}
