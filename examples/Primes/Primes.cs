/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using Qt.DotNet;
using Qt.DotNet.Utils;
using Qt.Quick;

namespace PrimesApp
{
    public class Primes : ListModel<Prime>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<Prime> Items { get; } = new();

        public int Count
        {
            get => Items.Count;
            set
            {
                if (value == Items.Count)
                    return;
                Items.Clear();
                Items.AddRange(Enumerable.Range(0, value)
                    .Select(i => new Prime() { N = i + 1 }));
                PropertyChanged?.Invoke(this, new(nameof(Count)));
            }
        }

        public override int ItemCount() => Count;

        public override Prime Data(int idx)
        {
            if (idx < 0 || idx >= Count)
                return null;
            return Items[idx];
        }
    }
}
