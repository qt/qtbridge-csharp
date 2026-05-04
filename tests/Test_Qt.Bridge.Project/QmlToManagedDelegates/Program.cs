// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QmlToManagedDelegates
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }

    public delegate int IntTransform(int value);

    public class Callback
    {
        public int InvokeSingle(IntTransform callback)
        {
            return callback?.Invoke(42) ?? -1;
        }
    }
}
