// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

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
