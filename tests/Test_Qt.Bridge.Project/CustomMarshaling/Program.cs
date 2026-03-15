// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Runtime.InteropServices;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace CustomMarshaling
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class Date
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Year = "";
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Month = "";
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Day = "";
    }

    public static class FunctionExports
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public delegate string FormatNumberDelegate(
            [In, MarshalAs(UnmanagedType.LPWStr)] string format,
            int number);

        public static string FormatNumber(string format, int number)
        {
            return string.Format(format, number);
        }

        [return: MarshalAs(UnmanagedType.LPWStr)]
        public delegate string FormatDateDelegate(
            [In, MarshalAs(UnmanagedType.LPWStr)] string format, [In] Date date);

        public static string FormatDate(string format, Date date)
        {
            return string.Format(format, date.Year, date.Month, date.Day);
        }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("CustomMarshaling managed app ready");
            return 0;
        }
    }
}
