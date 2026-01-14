// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Reflection;
using Qt.Quick;

namespace PrimesApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("Main");
            Qml.WaitForExit();
        }

        public void Load(string uri, string type)
        {
            _ = Task.Run(() => Qml.LoadFromModule(uri, type));
        }
    }
}
