// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections;
using System.Collections.Concurrent;

namespace Qt.Bridge.Utils.Collections.Concurrent
{
    public class ConcurrentSet<T> : IReadOnlyCollection<T>
    {
        private ConcurrentDictionary<T, bool> Items { get; }
        public ConcurrentSet(IEqualityComparer<T> comparer = null)
        {
            Items = new ConcurrentDictionary<T, bool>(comparer ?? EqualityComparer<T>.Default);
        }
        public bool Add(T item) => Items.TryAdd(item, true);
        public bool Remove(T item) => Items.TryRemove(item, out _);
        public void Clear() => Items.Clear();

        public int Count => Items.Count;
        public bool Contains(T item) => Items.ContainsKey(item);

        public IEnumerator<T> GetEnumerator() => Items.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Items.Keys.GetEnumerator();
    }
}
