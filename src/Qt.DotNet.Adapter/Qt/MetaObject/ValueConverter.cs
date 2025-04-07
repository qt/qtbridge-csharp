/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Numerics;

namespace Qt.MetaObject
{
    internal static class ValueConverter
    {
        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private static Type GenericNumberBase { get; }
            = typeof(INumberBase<int>).GetGenericTypeDefinition();

        private static Type GetNumberBase(Type t)
        {
            return t.FindInterfaces((i, _) => i.IsGenericType
                && i.GetGenericTypeDefinition() == GenericNumberBase, null)
                .FirstOrDefault();
        }

        private static object CreateTruncating(object x, Type tyNum)
        {
            var genFunc = tyNum.GetMethod("CreateTruncating");
            var func = genFunc.MakeGenericMethod(x.GetType());
            return func?.Invoke(null, new object[] { x });
        }

        public static object ConvertValue(object x, Type ty)
        {
            var tx = x.GetType();

            // convert any type to the same type: return input value
            if (tx == ty)
                return x;

            // ref type to compatible ref type: return input value
            if (!tx.IsValueType && !ty.IsValueType && ty.IsAssignableFrom(tx))
                return x;

            bool txIsNum = GetNumberBase(tx) != null;
            var tyNum = GetNumberBase(ty);
            bool tyIsNum = tyNum != null;

            try {
                return x switch
                {
                    // bool to any number type: true is 1, false is 0
                    bool xBool when tyIsNum
                        => CreateTruncating(xBool ? 1 : 0, tyNum),

                    // string to char: first char in string, or '\0' if empty string
                    string xStr when ty == typeof(char)
                        => xStr.Length > 0 ? Convert.ChangeType(xStr[0], ty) : GetDefault(ty),

                    // string to bool: case-insensitive comparison with the string "True"
                    string xStr when ty == typeof(bool)
                        => Convert.ChangeType(xStr.Equals(bool.TrueString,
                            StringComparison.OrdinalIgnoreCase), ty),

                    // string to any number type: parse as decimal and truncate to target number
                    // type, or 0 if parse fails
                    string xStr when tyIsNum
                        => decimal.TryParse(xStr, out decimal y)
                            ? CreateTruncating(y, tyNum) : GetDefault(ty),

                    // any type to bool: false if input is default value, true otherwise
                    _ when ty == typeof(bool)
                        => Convert.ChangeType(!x.Equals(GetDefault(tx)), ty),

                    // any number type to char: truncate to ushort then reinterpret as char
                    _ when txIsNum && ty == typeof(char)
                        => Convert.ChangeType(CreateTruncating(x, typeof(ushort)), ty),

                    // any number type to number type: truncate to target type
                    _ when txIsNum && tyIsNum
                        => CreateTruncating(x, tyNum),

                    // any type to string: convert to string
                    _ when ty == typeof(string)
                        => Convert.ToString(x),

                    // any type to any type: reinterpret as target type
                    _
                        => Convert.ChangeType(x, ty)
                };
            } catch (Exception) {
                // conversion failed: return default value
                return GetDefault(ty);
            }
        }
    }
}
