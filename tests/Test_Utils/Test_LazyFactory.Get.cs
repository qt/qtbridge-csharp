// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.Utils;

namespace Test_Utils
{
    public partial class Test_LazyFactory
    {
        // 1. Instance property default behavior
        [TestMethod]
        public void Get_InstanceProperty_DefaultsToDefaultValue_PerInstance()
        {
            var factory = new LazyFactory();
            var p1 = new Person { Age = 99 };
            Assert.AreEqual(0, factory.Get(() => p1.Age));

            var person = new Person { Age = 50 };
            Assert.AreEqual(0, factory.Get(() => person.Age));
        }

        // 2. Instance property with inline initFunc
        [TestMethod]
        public void Get_InstanceProperty_WithInitFunc_IsolatedPerInstance()
        {
            var factory = new LazyFactory();
            var person = new Person();
            Assert.AreEqual("Alice", factory.Get(() => person.Name, () => "Alice"));
            // Subsequent initFunc ignored
            Assert.AreEqual("Alice", factory.Get(() => person.Name, () => "Bob"));

            var person2 = new Person();
            Assert.IsNull(factory.Get(() => person2.Name));
            Assert.AreEqual("Bob", factory.Get(() => person2.Name, () => "Bob"));
        }

        // 3. Static property default behavior
        [TestMethod]
        public void Get_StaticProperty_DefaultsToDefaultValue()
        {
            var factory = new LazyFactory();
            Assert.IsFalse(factory.Get(() => AppSettings.IsFeatureEnabled));
            Assert.IsFalse(factory.Get(() => AppSettings.IsFeatureEnabled));
        }

        // 4. Static property with initFunc
        [TestMethod]
        public void Get_StaticProperty_WithInitFunc_IgnoredAfterFirstCall()
        {
            var factory = new LazyFactory();
            Assert.AreEqual(0.75, factory.Get(() => AppSettings.Threshold, () => 0.75));
            Assert.AreEqual(0.75, factory.Get(() => AppSettings.Threshold, () => 0.25));
        }

        // 5. Nested instance property behavior
        [TestMethod]
        public void Get_NestedInstanceProperty_IsolatedPerInstance()
        {
            var factory = new LazyFactory();
            var order1 = new Order
            {
                Customer = new Customer
                {
                    Address = new Address
                    {
                        City = "X"
                    }
                }
            };
            Assert.IsNull(factory.Get(() => order1.Customer.Address.City));
            Assert.AreEqual("XX", factory.Get(() => order1.Customer.Address.City, () => "XX"));

            var order2 = new Order
            {
                Customer = new Customer
                {
                    Address = new Address
                    {
                        City = "Y"
                    }
                }
            };
            Assert.IsNull(factory.Get(() => order2.Customer.Address.City));
            Assert.AreEqual("YY", factory.Get(() => order2.Customer.Address.City, () => "YY"));
        }

        // 6. Method-group initFunc
        [TestMethod]
        public void Get_MethodGroupInitFunc_WorksPerInstance()
        {
            var factory = new LazyFactory();
            var person = new Person();
            var expected = new DateTime(2025, 1, 1);
            Assert.AreEqual(expected, factory.Get(() => person.BirthDate, FetchReleaseDate));

            var person2 = new Person();
            Assert.AreEqual(expected, factory.Get(() => person2.BirthDate, FetchReleaseDate));
        }

        // 7. Explicit generic type parameter
        [TestMethod]
        public void Get_ExplicitGenericParameter_IsolatedPerInstance()
        {
            var factory = new LazyFactory();
            var p = new Person();
            Assert.AreEqual(42, factory.Get(() => p.Score, () => 42));

            var pB = new Person();
            Assert.AreEqual(0, factory.Get(() => pB.Score));
        }

        // 8. Capturing local variable in initFunc
        [TestMethod]
        public void Get_CapturedLocalVariable_IsolatedPerInstance()
        {
            var factory = new LazyFactory();
            const int x = 10;
            var person = new Person();
            Assert.AreEqual(30, factory.Get(() => person.Score, () => x * 3));

            var parson2 = new Person();
            Assert.AreEqual(0, factory.Get(() => parson2.Score));
        }

        // 9. Fallback via MethodCall -> compile & invoke
        [TestMethod]
        public void Get_FallbackMethodCall_IsolatedPerInstance()
        {
            var factory = new LazyFactory();

            valueHolder = new ValueHolder { Value = 123 };
            Assert.AreEqual(123, factory.Get(() => GetHolder()!.Value, () => 999));

            valueHolder = new ValueHolder { Value = 456 };
            Assert.AreEqual(456, factory.Get(() => GetHolder()!.Value, () => 888));
        }
    }
}
