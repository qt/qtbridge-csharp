// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Reflection;
using Qt.Bridge.Models;
using Qt.Quick;

[assembly: Qt.Generate(
    Packages = "QuickTest CorePrivate", Libraries = "Qt6::QuickTest PRIVATE Qt::CorePrivate")]

namespace Test_QtQuickTest
{
    internal class Program
    {
        static int Main(string[] args)
        {
            {
                Qml.WaitForExit();
                return 0;
            }
        }
    }
}
