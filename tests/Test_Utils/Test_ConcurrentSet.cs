// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only

namespace Test_Utils
{
    using Qt.Bridge.Utils.Collections.Concurrent;

    [TestClass]
    public class Test_ConcurrentSet
    {
        [TestMethod]
        public void Add_ShouldReturnTrue_OnFirstAdd_AndFalse_OnDuplicate()
        {
            var set = new ConcurrentSet<int>();
            Assert.IsTrue(set.Add(42), "First Add should succeed");
            Assert.IsTrue(set.Contains(42), "Set should contain the item after Add");
            Assert.IsFalse(set.Add(42), "Adding duplicate should return false");
        }

        [TestMethod]
        public void Remove_ShouldReturnTrue_WhenPresent_AndFalse_WhenAbsent()
        {
            var set = new ConcurrentSet<string>();
            Assert.IsFalse(set.Remove("x"), "Remove on empty should return false");
            Assert.IsTrue(set.Add("x"), "First Add should succeed");
            Assert.IsTrue(set.Remove("x"), "Remove of existing should return true");
            Assert.IsFalse(set.Contains("x"), "Item should no longer be in the set");
            Assert.IsFalse(set.Remove("x"), "Removing again should return false");
        }

        [TestMethod]
        public void Contains_OnEmptySet_ShouldReturnFalse()
        {
            var set = new ConcurrentSet<double>();
            Assert.IsFalse(set.Contains(3.14));
        }

        [TestMethod]
        public void Enumeration_ShouldYieldAllItems_WithoutDuplicates()
        {
            var items = new[] { 1, 2, 3, 4, 5 };
            var set = new ConcurrentSet<int>();
            foreach (var i in items)
                _ = set.Add(i);
            Assert.IsFalse(set.Add(3), "Add should fail");

            var list = set.ToList();
            CollectionAssert.AreEquivalent(items, list, "Enumeration should return unique items");
        }

        [TestMethod]
        public void CustomComparer_ShouldBeRespected()
        {
            var set = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);
            Assert.IsTrue(set.Add("hello"), "First Add should succeed");
            Assert.IsFalse(set.Add("HELLO"), "Comparer should treat 'HELLO' as duplicate");
            Assert.IsTrue(set.Contains("HeLlO"), "Contains should be case‐insensitive");

            var list = set.ToList();
            Assert.HasCount(1, list, "Only one entry should remain");
            Assert.AreEqual("hello", list[0], "Stored key is the first inserted value");
        }

        [TestMethod]
        public void Add_Remove_And_Contains_ShouldThrow_OnNull_ForRefTypes()
        {
            var set = new ConcurrentSet<string>();
            _ = Assert.ThrowsExactly<ArgumentNullException>(() => set.Add(null!));
            _ = Assert.ThrowsExactly<ArgumentNullException>(() => set.Remove(null!));
            _ = Assert.ThrowsExactly<ArgumentNullException>(() => set.Contains(null!));
        }

        [TestMethod]
        public void Concurrent_Adds_FromMultipleThreads_ShouldEndUpWithUniqueItems()
        {
            var set = new ConcurrentSet<int>();
            const int threadCount = 10, itemsPerThread = 1_000;

            _ = Parallel.For(0, threadCount, _ => {
                for (var i = 0; i < itemsPerThread; i++)
                    set.Add(i);    // each thread adds 0...999
            });

            // We expect exactly itemsPerThread unique values
            Assert.AreEqual(itemsPerThread, set.Count, "After concurrent adds there should be "
                + "exactly one of each item");

            // And each value 0...itemsPerThread-1 is present
            CollectionAssert.AreEquivalent(Enumerable.Range(0, itemsPerThread).ToList(), set.ToList());
        }

        [TestMethod]
        public void Concurrent_Removes_FromMultipleThreads_ShouldRemoveOnlyWhatExists()
        {
            var set = new ConcurrentSet<int>();
            const int threadCount = 4, itemsPerThread = 1000;

            // Populate with 0...999
            for (var i = 0; i < itemsPerThread; i++)
                _ = set.Add(i);

            // Each thread will try to remove 0...1499
            _ = Parallel.For(0, threadCount, _ => {
                for (var i = 0; i < itemsPerThread + 500; i++)
                    set.Remove(i);
            });

            // After all removals, we expect no items < 1000 remain...
            Assert.IsFalse(set.Any(x => x < itemsPerThread), "All originally present items should "
                + "have been removed.");

            // ...and no exception should have occurred
            Assert.AreEqual(0, set.Count, "Set should be empty at the end of concurrent removes.");
        }

        [TestMethod]
        public async Task Enumeration_While_Mutating_ShouldNotThrow_And_ReflectSnapshot()
        {
            const int initialCount = 5000;
            var set = new ConcurrentSet<int>();
            for (var i = 0; i < initialCount; i++)
                _ = set.Add(i);

            var cts = new CancellationTokenSource();

            var task = Task.Run(() =>
            {
                var rnd = new Random();
                while (!cts.IsCancellationRequested) {
                    _ = set.Add(rnd.Next(initialCount * 2));
                    _ = set.Remove(rnd.Next(initialCount * 2));
                }
            });

            var snapshot = set.ToList();  // must not throw

            await cts.CancelAsync(); // cancel
            await task;  // and await the task

            Assert.AreEqual(snapshot.Distinct().Count(), snapshot.Count, "Snapshot enumeration "
                + "should not yield duplicates.");
            Assert.IsTrue(snapshot.All(x => x is >= 0 and < initialCount * 2), "Enumerated items "
                + "should be in the expected range.");
        }

        [TestMethod]
        public void VeryLargeVolume_Parallel_AddsAndRemoves_ShouldMaintainCorrectCount()
        {
            var set = new ConcurrentSet<int>();
            const int threads = 8;
            const int opsPerThread = 250_000;  // total ops ~2M

            _ = Parallel.For(0, threads, tid => {
                var baseVal = tid * opsPerThread;
                // each thread adds a non-overlapping range
                for (var i = baseVal; i < baseVal + opsPerThread; i++)
                    _ = set.Add(i);

                // and then removes half of them
                for (var i = baseVal; i < baseVal + opsPerThread / 2; i++)
                    _ = set.Remove(i);
            });

            // Expect exactly threads * opsPerThread/2 items remaining
            const int expected = threads * (opsPerThread / 2);
            Assert.AreEqual(expected, set.Count, "After Add then Remove, there should be "
                + $"{expected} items left.");
        }

        [TestMethod]
        public void Count_ShouldReflectNumberOfItems()
        {
            var set = new ConcurrentSet<string>();
            Assert.AreEqual(0, set.Count);

            Assert.IsTrue(set.Add("apple"));
            Assert.IsTrue(set.Add("banana"));
            Assert.AreEqual(2, set.Count, "After two times Add, the count is expected to be 2.");

            Assert.IsTrue(set.Remove("apple"));
            Assert.AreEqual(1, set.Count, "After one Remove, the count is expected to be 1.");
        }

        [TestMethod]
        public void Clear_ShouldRemoveAllItems()
        {
            var set = new ConcurrentSet<int>();
            for (var i = 0; i < 10; i++)
                Assert.IsTrue(set.Add(i));

            Assert.AreEqual(10, set.Count, "After ten times Add, the count is expected to be 10.");
            set.Clear();
            Assert.AreEqual(0, set.Count, "Count should be 0 after Clear");

            // Contains must return false for anything
            Assert.IsFalse(set.Contains(5));
            Assert.IsFalse(set.Contains(0));
        }

        [TestMethod]
        public void IReadOnlyCollection_Interface_Works()
        {
            // Exercise the IReadOnlyCollection<T> interface
            IReadOnlyCollection<int> readOnly = (ConcurrentSet<int>) [];
            Assert.IsEmpty(readOnly);

            // Cast must succeed and we should be able to Add
            var set = (ConcurrentSet<int>)readOnly;
            Assert.IsTrue(set.Add(42));

            Assert.HasCount(1, readOnly, "Count on IReadOnlyCollection should reflect Set count");
            CollectionAssert.AreEqual((int[]) [42], readOnly.ToList());
        }
    }
}
