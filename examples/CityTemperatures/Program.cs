/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;
using Qt.Quick;

namespace CityTemperatures
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("CityTemperatures");
            Qml.WaitForExit();
        }
    }
}
