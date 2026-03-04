// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    internal class UriMarshaler : CustomMarshaler<Uri>
    {
        private static UriMarshaler Instance { get; } = new();

        public static ICustomMarshaler GetInstance(string options) => Instance;

        public override int NativeSize { get; } = Marshal.SizeOf<nint>();

        public override Uri MarshalIn(nint ptr)
        {
            // QUrl custom marshaling uses UTF-16 buffers on all platforms.
            var url = Marshal.PtrToStringUni(ptr);
            Marshal.FreeHGlobal(ptr);
            return new Uri(url);
        }

        public override nint MarshalOut(Uri uri)
        {
            return Marshal.StringToHGlobalUni(uri.ToString());
        }
    }
}
