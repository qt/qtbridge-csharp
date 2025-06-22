/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet
{
    public interface IQModelIndex
    {
        bool IsValid();
        int Row();
        int Column();
        IntPtr InternalPointer();
    }

    public partial class Adapter
    {
        public partial interface IStatic
        {
            IQModelIndex QModelIndex_Create();
        }
        public static IQModelIndex QModelIndex() => Static.QModelIndex_Create();
    }

    public static class QModelIndex
    {
        public static IQModelIndex Create() => Adapter.Static?.QModelIndex_Create();
    }
}
