// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    internal class StringMarshaler : CustomMarshaler<string>
    {
        private static StringMarshaler Instance { get; } = new();

        public static ICustomMarshaler GetInstance(string _) => Instance;

        public override int NativeSize { get; } = Marshal.SizeOf<nint>();

        public override string MarshalIn(nint ptr)
        {
            var str = Marshal.PtrToStringUni(ptr);
            Marshal.FreeHGlobal(ptr);
            return str;
        }

        public override nint MarshalOut(string str)
        {
            return Marshal.StringToHGlobalUni(str);
        }
    }
}
