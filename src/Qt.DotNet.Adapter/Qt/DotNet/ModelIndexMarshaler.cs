/**************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    internal class ModelIndexMarshaler : CustomMarshaler<ModelIndex>
    {
        private static ModelIndexMarshaler Instance { get; } = new();

        public static ICustomMarshaler GetInstance(string options) => Instance;

        public override int NativeSize { get; } = Marshal.SizeOf<ModelIndex>();

        public override ModelIndex MarshalIn(nint ptr)
        {
            return Marshal.PtrToStructure<ModelIndex>(ptr);
        }

        public override nint MarshalOut(ModelIndex idx)
        {
            var ptr = Marshal.AllocHGlobal(NativeSize);
            Marshal.StructureToPtr(idx, ptr, false);
            return ptr;
        }

        public override void CleanUp(nint ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
