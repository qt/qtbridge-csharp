/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;

namespace Qt.MetaObject
{
    public enum MetaObjectFeatures { Full, Gadget }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = false)]
    public sealed class QObjectAttribute : Attribute
    {
        public string Name { get; set; }
        public MetaObjectFeatures Features { get; private set; }

        public QObjectAttribute(MetaObjectFeatures features = MetaObjectFeatures.Full)
        {
            Features = features;
        }
    }
}
