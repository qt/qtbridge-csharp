/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet
{
    public interface IQVariant
    {
        void SetValue(bool value);
        void SetValue(int value);
        void SetValue(uint value);
        void SetValue(long value);
        void SetValue(ulong value);
        void SetValue(float value);
        void SetValue(double value);
        void SetValue(char value);
        void SetValue(string value);
        bool CanConvertToBool();
        bool CanConvertToInt();
        bool CanConvertToUInt();
        bool CanConvertToLongLong();
        bool CanConvertToULongLong();
        bool CanConvertToFloat();
        bool CanConvertToDouble();
        bool CanConvertToChar();
        bool CanConvertToString();
        bool ToBool();
        int ToInt();
        uint ToUInt();
        long ToLongLong();
        ulong ToULongLong();
        float ToFloat();
        double ToDouble();
        char ToChar();
        string ToStringValue();
    }

    public partial class Adapter
    {
        public partial interface IStatic
        {
            IQVariant QVariant_Create();
        }
        public static IQVariant QVariant() => Static.QVariant_Create();

        public static IQVariant QVariant<T>(T value)
        {
            var v = QVariant();
            if (value is bool boolValue)
                v.SetValue(boolValue);
            else if (value is int intValue)
                v.SetValue(intValue);
            else if (value is uint uintValue)
                v.SetValue(uintValue);
            else if (value is long longValue)
                v.SetValue(longValue);
            else if (value is ulong ulongValue)
                v.SetValue(ulongValue);
            else if (value is float floatValue)
                v.SetValue(floatValue);
            else if (value is double doubleValue)
                v.SetValue(doubleValue);
            else if (value is char charValue)
                v.SetValue(charValue);
            else if (value is string stringValue)
                v.SetValue(stringValue);
            else
                throw new InvalidCastException($"Unsupported QVariant type: {typeof(T).FullName}");

            return v;
        }
    }
}
