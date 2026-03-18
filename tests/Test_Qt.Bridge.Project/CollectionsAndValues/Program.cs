// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;

using Qt.DotNet;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace CollectionsAndValues
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("CollectionsAndValues managed app ready");
            return 0;
        }
    }

    public sealed class CollectionsFixture
    {
        public const int FooNumber = 42;
        public const string FooString = "FOO";

        public int FooField = FooNumber;
        public static int FooStaticField = -FooNumber;

        public CollectionsFixture()
        {}

        public static ModelIndex FindIndex()
        {
            return new(42, 24, 0x12345678);
        }

        public static string DataAt(ModelIndex idx)
        {
            return $"{idx.Row}, {idx.Column}, 0x{idx.Id:X}";
        }

        public static DateTime GetDateTime()
        {
            return new DateTime(1912, 6, 23, 11, 22, 33, 444);
        }

        public static string PrintDateTime(DateTime t)
        {
            return $"{t.Year:0000}-{t.Month:00}-{t.Day:00} {t.Hour:00}:{t.Minute:00}:"
                + $"{t.Second:00}.{t.Millisecond:000}";
        }

        public static Uri GetUri()
        {
            return new Uri("https://www.qt.io/developers#wiki");
        }

        public static string PrintUri(Uri uri)
        {
            return uri.ToString();
        }
    }
}
