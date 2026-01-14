// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections;
using System.Collections.Generic;

namespace Qt.Bridge.Utils.Collections.Concurrent
{
    public interface IPrioritizable<TPrio>
        where TPrio : IComparable<TPrio>, IEquatable<TPrio>
    {
        TPrio Priority { get; }
    }

    public class ConcurrentPriorityList<T, TPrio> : IEnumerable<T>
        where T : IPrioritizable<TPrio>
        where TPrio : IComparable<TPrio>, IEquatable<TPrio>
    {
        private readonly object criticalSection = new();
        private ulong timestamp = 0;

        private class PriorityComparer : IComparer<(TPrio Prio, ulong Timestamp)>
        {
            public int Compare((TPrio Prio, ulong Timestamp) x, (TPrio Prio, ulong Timestamp) y)
            {
                if (!x.Prio.Equals(y.Prio))
                    return x.Prio.CompareTo(y.Prio);
                return x.Timestamp.CompareTo(y.Timestamp);
            }
        }

        private SortedList<(TPrio Prio, ulong Timestamp), T> items = new(new PriorityComparer());
        private IEnumerable<T> Items
        {
            get
            {
                lock (criticalSection)
                    return items.Values.ToList();
            }
        }
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();

        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            lock (criticalSection)
                items.Add((item.Priority, ++timestamp), item);
        }

        public void Clear()
        {
            lock (criticalSection)
                items.Clear();
        }
    }
}
