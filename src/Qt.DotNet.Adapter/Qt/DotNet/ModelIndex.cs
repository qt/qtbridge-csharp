/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    [StructLayout(LayoutKind.Sequential)]
    public class ModelIndex
    {
        public int Row = -1;
        public int Column = -1;
        public nint Id = nint.Zero;

        /// <summary>
        /// Pointer to the QAIM instance that owns the index. For ModelIndex objects created outside
        /// of the scope of a QAIM-based class, this will be set to an invalid pointer value of -1.
        /// This will signal the QAIM interop layer that ownership needs to be set before the index
        /// can be used.
        /// </summary>
        public nint Model = -1;

        public ModelIndex() : this(-1, -1, nint.Zero, -1)
        {
        }

        public ModelIndex(int r, int c) : this(r, c, nint.Zero, -1)
        {
        }

        public ModelIndex(int r, int c, nint i) : this(r, c, i, -1)
        {
        }

        public ModelIndex(int r, int c, nint i, nint m)
        {
            Row = r;
            Column = c;
            Id = i;
            Model = m;
        }

        public bool IsValid => Row >= 0 && Column >= 0 && Model != nint.Zero;

        public static readonly ModelIndex Empty = new();
    }
}
