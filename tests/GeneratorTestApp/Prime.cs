/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using Qt.DotNet.Utils;

namespace GeneratorTestApp
{
    public class Prime : INotifyPropertyChanged
    {
        public List<Prime > Primes { get; set; }
        public Prime()
        {
            lazy.PropertyChanged += OnPropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => lazy.PropertyChanged += value;
            remove => lazy.PropertyChanged -= value;
        }

        public int Index
        {
            get => lazy.Get(() => Index, () => 0);
            set => lazy.Set(() => Index, value);
        }

        public int Value
        {
            get => lazy.Get(() => Value, () => 2);
            private set => lazy.Set(() => Value, value);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Index))
                Value = NthPrime(Index + 1);
        }

        private LazyFactory lazy = new();

        private static int NthPrime(int n)
        {
            int x = 2;
            while (n > 0) {
                if (IsPrime(x))
                    --n;
                ++x;
            }
            return x - 1;
        }

        private static bool IsPrime(int x)
        {
            if (x <= 1)
                return false;
            if (x == 2 || x == 3)
                return true;
            if (x % 2 == 0 || x % 3 == 0)
                return false;
            for (int i = 5; i * i <= x; i += 6)
                if (x % i == 0 || x % (i + 2) == 0)
                    return false;
            return true;
        }
    }
}
