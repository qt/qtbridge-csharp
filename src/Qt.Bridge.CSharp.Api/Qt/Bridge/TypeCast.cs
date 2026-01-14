// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using Qt.Quick;

namespace Qt.Bridge
{
    [Include]
    [QmlElement(Singleton = true)]
    public class TypeCast
    {
        [Enable]
        public TypeCast() { }
    }
}
