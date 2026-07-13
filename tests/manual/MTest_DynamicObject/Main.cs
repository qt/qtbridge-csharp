// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Quick;

[assembly: Qt.Generate(
    MainStartingUp = """
    qputenv("QT_QPA_PLATFORM", "windows:darkmode=0");
    """)]

namespace MTest_DynamicObject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("Main");
            Qml.WaitForExit();
        }
    }
}
