// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;

namespace PrimesApp
{
    public class PrimeCreateEventArgs : EventArgs
    {
        public List<Prime> Primes { get; set; }
    }

    public class PrimeFactory
    {
        public int GetNthPrime(int n)
        {
            return Prime.NthPrime(n);
        }

        public Prime[] GetNPrimes(int n)
        {
            var primes = new List<Prime>();
            for (int i = 0; i < n; i++) {
                primes.Add(new Prime() { N = i + 1 });
            }
            return primes.ToArray();
        }

        public int[] GetNPrimeValues(int n)
        {
            var primes = new List<int>();
            for (int i = 0; i < n; i++) {
                primes.Add(GetNthPrime(i + 1));
            }
            return primes.ToArray();
        }

        public event EventHandler<PrimeCreateEventArgs> PrimesCreated;

        public void CreateNPrimes(int n)
        {
            var primes = GetNPrimes(n);
            PrimesCreated?.Invoke(this, new() { Primes = primes.ToList() });
        }
    }
}
