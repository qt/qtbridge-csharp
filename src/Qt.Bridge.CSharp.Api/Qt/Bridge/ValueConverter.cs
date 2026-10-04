// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Qt.DotNet;

namespace Qt.Bridge
{
    public static class ValueConverter
    {
        public static object FromBoolean(bool value) => value;
        public static object FromSByte(sbyte value) => value;
        public static object FromByte(byte value) => value;
        public static object FromInt16(short value) => value;
        public static object FromUInt16(ushort value) => value;
        public static object FromInt32(int value) => value;
        public static object FromUInt32(uint value) => value;
        public static object FromInt64(long value) => value;
        public static object FromUInt64(ulong value) => value;
        public static object FromSingle(float value) => value;
        public static object FromDouble(double value) => value;
        public static object FromDateTime(DateTime value) => value;
        public static object FromUri(Uri value) => value;
        public static object FromModelIndex(ModelIndex value) => value;
        public static object FromChar(char value) => value;
        public static object FromString(string value) => value;

        internal static bool IsValue<T>(object obj) => obj is T;
        public static bool IsBoolean(object obj) => obj is bool;
        public static bool IsSByte(object obj) => obj is sbyte;
        public static bool IsByte(object obj) => obj is byte;
        public static bool IsInt16(object obj) => obj is short;
        public static bool IsUInt16(object obj) => obj is ushort;
        public static bool IsInt32(object obj) => obj is int;
        public static bool IsUInt32(object obj) => obj is uint;
        public static bool IsInt64(object obj) => obj is long;
        public static bool IsUInt64(object obj) => obj is ulong;
        public static bool IsSingle(object obj) => obj is float;
        public static bool IsDouble(object obj) => obj is double;
        public static bool IsDateTime(object obj) => obj is DateTime;
        public static bool IsUri(object obj) => obj is Uri;
        public static bool IsModelIndex(object obj) => obj is ModelIndex;
        public static bool IsChar(object obj) => obj is char;
        public static bool IsString(object obj) => obj is string;

        /// <summary>
        /// Built-in types supported by
        /// <see href="https://doc.qt.io/qt-6/qmetatype.html#Type-enum">QMetaType</see>
        /// </summary>
        /// <remarks>
        /// Copied from
        /// <see href="https://code.qt.io/cgit/qt/qtbase.git/tree/src/corelib/kernel/qmetatype.h?h=6.12.0#n418"
        /// >$QtBase/src/corelib/kernel/qmetatype.h</see>
        /// </remarks>
        public enum QMetaType : int
        {
            UnknownType = 0, Bool = 1, Int = 2, UInt = 3, LongLong = 4, ULongLong = 5,
            Double = 6, Long = 32, Short = 33, Char = 34, ULong = 35, UShort = 36,
            UChar = 37, Float = 38,
            VoidStar = 31,
            QChar = 7, QString = 10, QStringList = 11, QByteArray = 12,
            QBitArray = 13, QDate = 14, QTime = 15, QDateTime = 16, QUrl = 17,
            QLocale = 18, QRect = 19, QRectF = 20, QSize = 21, QSizeF = 22,
            QLine = 23, QLineF = 24, QPoint = 25, QPointF = 26,
            QEasingCurve = 29, QUuid = 30, QVariant = 41, QModelIndex = 42,
            QPersistentModelIndex = 50, QRegularExpression = 44,
            QJsonValue = 45, QJsonObject = 46, QJsonArray = 47, QJsonDocument = 48,
            QByteArrayList = 49, QObjectStar = 39, SChar = 40,
            Void = 43,
            Nullptr = 51,
            QVariantMap = 8, QVariantList = 9, QVariantHash = 28, QVariantPair = 58,
            QCborSimpleType = 52, QCborValue = 53, QCborArray = 54, QCborMap = 55,
            Char16 = 56, Char32 = 57,
            Int128 = 59, UInt128 = 60, Float128 = 61, BFloat16 = 62, Float16 = 63,

            // Gui types
            QFont = 0x1000, QPixmap = 0x1001, QBrush = 0x1002, QColor = 0x1003, QPalette = 0x1004,
            QIcon = 0x1005, QImage = 0x1006, QPolygon = 0x1007, QRegion = 0x1008, QBitmap = 0x1009,
            QCursor = 0x100a, QKeySequence = 0x100b, QPen = 0x100c, QTextLength = 0x100d, QTextFormat = 0x100e,
            QTransform = 0x1010, QMatrix4x4 = 0x1011, QVector2D = 0x1012,
            QVector3D = 0x1013, QVector4D = 0x1014, QQuaternion = 0x1015, QPolygonF = 0x1016, QColorSpace = 0x1017,

            // Widget types
            QSizePolicy = 0x2000,

            // Start-point for client-code types:
            User = 65536
        }

        public static int ValueTypeId(object obj)
        {
            return (int)(obj switch
            {
                bool => QMetaType.Bool,
                sbyte => QMetaType.SChar,
                byte => QMetaType.UChar,
                short => QMetaType.Short,
                ushort => QMetaType.UShort,
                int => QMetaType.Int,
                uint => QMetaType.UInt,
                long => QMetaType.LongLong,
                ulong => QMetaType.ULongLong,
                float => QMetaType.Float,
                double => QMetaType.Double,
                DateTime => QMetaType.QDateTime,
                char => QMetaType.QChar,
                string => QMetaType.QString,
                Uri => QMetaType.QUrl,
                ModelIndex => QMetaType.QModelIndex,
                _ => QMetaType.QObjectStar
            });
        }

        internal static T ToValue<T>(object obj, T defaultValue = default)
        {
            if (obj == null)
                return defaultValue;
            if (IsValue<T>(obj))
                return (T)obj;
            try {
                return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
            } catch (Exception e) when (e is InvalidCastException
                or FormatException or OverflowException) {
                return defaultValue;
            }
        }

        public static bool ToBoolean(object obj) => ToValue<bool>(obj);
        public static sbyte ToSByte(object obj) => ToValue<sbyte>(obj);
        public static byte ToByte(object obj) => ToValue<byte>(obj);
        public static short ToInt16(object obj) => ToValue<short>(obj);
        public static ushort ToUInt16(object obj) => ToValue<ushort>(obj);
        public static int ToInt32(object obj) => ToValue<int>(obj);
        public static uint ToUInt32(object obj) => ToValue<uint>(obj);
        public static long ToInt64(object obj) => ToValue<long>(obj);
        public static ulong ToUInt64(object obj) => ToValue<ulong>(obj);
        public static float ToSingle(object obj) => ToValue<float>(obj);
        public static double ToDouble(object obj) => ToValue<double>(obj);
        public static DateTime ToDateTime(object obj) => ToValue<DateTime>(obj);
        public static Uri ToUri(object obj) => ToValue<Uri>(obj);
        public static ModelIndex ToModelIndex(object obj) => ToValue<ModelIndex>(obj);
        public static char ToChar(object obj) => ToValue<char>(obj);
        public static string ToString(object obj) => obj as string ?? obj?.ToString() ?? "";

        public static object[] ToArray(object obj)
        {
            var result = new List<object>();
            switch(obj) {
            case IDictionary dict:
                foreach (DictionaryEntry entry in dict) {
                    result.Add(entry.Key);
                    result.Add(entry.Value);
                }
                break;
            case IEnumerable list and not string:
                result.AddRange(list.Cast<object>());
                break;
            default:
                result.Add(obj);
                break;
            }
            return result.ToArray();
        }

        public static bool IsConvertible(Type t)
        {
            return t == typeof(bool)
                || t == typeof(sbyte)
                || t == typeof(byte)
                || t == typeof(short)
                || t == typeof(ushort)
                || t == typeof(int)
                || t == typeof(uint)
                || t == typeof(long)
                || t == typeof(ulong)
                || t == typeof(float)
                || t == typeof(double)
                || t == typeof(DateTime)
                || t == typeof(Uri)
                || t == typeof(ModelIndex)
                || t == typeof(char)
                || t == typeof(string);
        }
    }
}
