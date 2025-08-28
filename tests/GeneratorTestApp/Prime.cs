/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using Qt.DotNet;
using Qt.DotNet.Utils;
using Qt.Quick;

namespace GeneratorTestApp
{
    public class Prime : INotifyPropertyChanged
    {
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

        public static int NthPrime(int n)
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

    public class PrimeCreateEventArgs : EventArgs
    {
        public List<Prime> Primes { get; set; }
    }

    public class PrimeFactory
    {
        public event EventHandler<PrimeCreateEventArgs> PrimeCreated;

        public Prime Prime { get; set; } = new();

        public List<Prime> Primes { get; set; } = new();

        public Prime GetNthPrime(int index)
        {
            var prime = new Prime() { Index = index };
            Primes.Add(prime);
            PrimeCreated?.Invoke(this, new() { Primes = Primes });
            return prime;
        }

        public bool IsValid(Prime prime)
        {
            return prime.Index > 0;
        }

        public Prime[] GetNPrimes(int n)
        {
            var primes = new List<Prime>();
            for (int i = 0; i < n; i++) {
                primes.Add(new Prime() { Index = i });
            }
            return primes.ToArray();
        }

        public int[] GetNPrimeValues(int n)
        {
            var primes = new List<int>();
            for (int i = 0; i < n; i++) {
                primes.Add(Prime.NthPrime(i));
            }
            return primes.ToArray();
        }
    }

    public class Primes : ListModel
    {
        public int Count { get; set; }

        private static class PrimeRoles
        {
            public const int Index = Roles.UserRole + 0;
            public const int Value = Roles.UserRole + 1;
        }

        private Dictionary<int, string> RoleMap { get; } = new()
        {
            { PrimeRoles.Index, "index" },
            { PrimeRoles.Value, "value" },
        };

        public override Dictionary<int, string> RoleNames()
        {
            return RoleMap;
        }

        public override int RowCount(ModelIndex parent)
        {
            return Count;
        }

        public override object Data(ModelIndex idx, int role)
        {
            if (idx.Row < 0 || idx.Row >= Count)
                return null;
            return role switch
            {
                PrimeRoles.Index => idx.Row + 1,
                PrimeRoles.Value => Prime.NthPrime(idx.Row + 1),
                _ => null
            };
        }
    }
}
