// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Text;

using Qt.DotNet;
using static Qt.Bridge.ValueConverter;

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

        public static string Echo(string value) => value;

        public static string NullString() => null;

        public static string EmptyString() => string.Empty;

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

        public static object FromTypeId(int typeId)
        {
            return (QMetaType)typeId switch
            {
                QMetaType.Bool => new bool(),
                QMetaType.SChar => new sbyte(),
                QMetaType.UChar => new byte(),
                QMetaType.Short => new short(),
                QMetaType.UShort => new ushort(),
                QMetaType.Int => new int(),
                QMetaType.UInt => new uint(),
                QMetaType.LongLong => new long(),
                QMetaType.ULongLong => new ulong(),
                QMetaType.Float => new float(),
                QMetaType.Double => new double(),
                QMetaType.QDateTime => new DateTime(),
                QMetaType.QChar => new char(),
                QMetaType.QString => "",
                QMetaType.QUrl => new Uri("https://qt.io"),
                QMetaType.QModelIndex => new ModelIndex(),
                QMetaType.QObjectStar => new StringBuilder(),
                _ => null
            };
        }
    }
}
