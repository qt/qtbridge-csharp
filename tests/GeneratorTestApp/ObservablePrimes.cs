/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.ObjectModel;

namespace GeneratorTestApp
{
    public class ObservablePrimes
    {
        public ObservableCollection<Prime> Items { get; } = [];

        public ObservablePrimes()
        {
            for (var i = 1; i <= 10; i++)
                Items.Add(new Prime { N = i });
        }

        public void AddNext()
        {
            var n = Items.Count + 1;
            Items.Add(new Prime { N = n });
        }

        public void RemoveLast()
        {
            if (Items.Count > 0)
                Items.RemoveAt(Items.Count - 1);
        }

        public void ReplaceFirst()
        {
            if (Items.Count > 0)
                Items[0] = new Prime { N = Items[0].N + 1000 };
        }

        public void MoveFirstToEnd()
        {
            if (Items.Count > 1)
                Items.Move(0, Items.Count - 1);
        }

        public void Reset()
        {
            Items.Clear();
        }

        public void IncrementFirstN()
        {
            if (Items.Count > 0)
                Items[0].N += 1;
        }

        public void RandomizeSomeNs()
        {
            if (Items.Count == 0)
                return;
            var rnd = new Random();
            for (var k = 0; k < Math.Min(3, Items.Count); ++k) {
                var i = rnd.Next(Items.Count);
                var delta = rnd.Next(-2, 3);
                Items[i].N = Math.Max(1, Items[i].N + delta);
            }
        }
    }
}
