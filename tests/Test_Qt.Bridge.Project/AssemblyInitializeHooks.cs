// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public static class AssemblyInitializeHooks
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            var qtDir = Environment
                .GetEnvironmentVariable("QtDir", EnvironmentVariableTarget.Process);
            if (!string.IsNullOrEmpty(qtDir)) {
                Console.WriteLine($"QTDIR is set: {qtDir}");
                if (OperatingSystem.IsWindows()) {
                    var sysQtDir = Environment.GetEnvironmentVariable(
                            "QtDir", EnvironmentVariableTarget.User)
                        ?? Environment.GetEnvironmentVariable(
                            "QtDir", EnvironmentVariableTarget.Machine);
                    if (qtDir != sysQtDir) {
                        Console.WriteLine($"Resetting QTDIR to: {sysQtDir}");
                        Environment.SetEnvironmentVariable(
                            "QtDir", sysQtDir, EnvironmentVariableTarget.Process);
                    }
                }
            }

            TempProject.CleanupProjectRoot();
        }
    }
}
