/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet
{
    public interface IQVariant
    {
        string ToStringValue();
        void SetValue(string value);
    }

    public partial class Adapter
    {
        public partial interface IStatic
        {
            IQVariant QVariant_Create();
            IQVariant QVariant_Create(string value);
        }
        public static IQVariant QVariant() => Static.QVariant_Create();
        public static IQVariant QVariant(string value) => Static.QVariant_Create(value);
    }

}
