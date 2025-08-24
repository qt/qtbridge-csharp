/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Reflection;

namespace Qt.DotNet.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool IsNotifiable(this PropertyInfo prop)
        {
            return prop.ReflectedType.Implements<INotifyPropertyChanged>();
        }

        public static bool IsStatic(this PropertyInfo prop)
        {
            return prop.GetAccessors().FirstOrDefault()?.IsStatic == true;
        }
    }
}
