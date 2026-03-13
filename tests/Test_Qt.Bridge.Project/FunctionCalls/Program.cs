// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Runtime.InteropServices;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace FunctionCalls
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("FunctionCalls managed app ready");
            return 0;
        }
    }

    public static class FunctionExports
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public delegate string FormatNumberDelegate(
            [In, MarshalAs(UnmanagedType.LPWStr)] string format,
            int value);

        public static string FormatNumber(string format, int value)
        {
            return string.Format(format, value);
        }

        public static int EntryPoint(IntPtr arg, int argLength)
        {
            return Convert.ToInt32(Marshal.PtrToStringUni(arg, argLength));
        }
    }
}
