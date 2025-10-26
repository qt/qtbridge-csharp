/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Qt.DotNet
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
