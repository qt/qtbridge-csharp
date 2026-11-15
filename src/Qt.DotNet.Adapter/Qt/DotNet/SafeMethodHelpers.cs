// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    public static class SafeMethodHelpers
    {
        /// <summary>
        /// Converts URL text inside a safe method, so an invalid URI uses its exception channel.
        /// </summary>
        public static Uri ParseUri(string value) =>
            value == null ? null : new Uri(value, UriKind.RelativeOrAbsolute);

        /// <summary>
        /// Writes a GCHandle for the caught exception to the address of the native caller's
        /// exception-handle variable. Successful calls leave its initial zero value unchanged.
        /// </summary>
        public static void SetException(IntPtr exceptionHandleAddress, Exception exception)
        {
            if (exceptionHandleAddress != IntPtr.Zero)
                Marshal.WriteIntPtr(exceptionHandleAddress, Adapter.GetRefPtrToObject(exception));
        }
    }
}
