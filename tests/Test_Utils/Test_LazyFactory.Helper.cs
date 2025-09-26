/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;

namespace Test_Utils
{
    public partial class Test_LazyFactory
    {
        private class Dummy
        {
            public int Prop { get; init; }
        }

        private class NotifyingDummy : INotifyPropertyChanged
        {
            private int prop;
            public int Prop
            {
                get => prop;
                set
                {
                    prop = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Prop)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private class Person
        {
            public int Age { get; init; }
            public string? Name { get; set; }
            public int Score { get; set; }
            public DateTime BirthDate { get; set; }
        }

        private class Address
        {
            public string? City { get; init; }
        }

        private class Customer
        {
            public Address? Address { get; init; }
        }

        private class Order
        {
            public Customer? Customer { get; init; }
        }

        private static class AppSettings
        {
            public static bool IsFeatureEnabled { get; set; }
            public static double Threshold { get; set; }
        }

        private class ValueHolder
        {
            public int Value { get; init; }
        }
        private ValueHolder? valueHolder;

        // Method used in fallback expression
        private ValueHolder? GetHolder() => valueHolder;

        // Helper for method-group init
        private static DateTime FetchReleaseDate() => new(2025, 1, 1);
    }
}
