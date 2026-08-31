// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Quick;
using System.Runtime.InteropServices;

[assembly: Qt.Generate(
    MainStartingUp = """
    #if defined(Q_OS_WIN)
        qputenv("QT_QPA_PLATFORM", "windows:darkmode=0");
    #elif defined(Q_OS_LINUX)
        qputenv("QT_QPA_PLATFORM", "xcb");
    #elif defined(Q_OS_MACOS)
        qputenv("QT_QPA_PLATFORM", "cocoa");
    #endif
        // Main.qml reads fixtures.json from disk via XMLHttpRequest,
        // which Qt disables for file:// URLs by default.
        qputenv("QML_XHR_ALLOW_FILE_READ", "1");
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
