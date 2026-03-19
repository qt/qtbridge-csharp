// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Threading;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace HostLifecycle
{
    internal class Program
    {
        public static bool KeepRunning { get; set; } = true;

        static int Main(string[] args)
        {
            Console.WriteLine("HostLifecycle managed app ready");

            while (KeepRunning)
                Thread.Sleep(50);

            Console.WriteLine("HostLifecycle managed app stopping");
            return 0;
        }
    }
}
