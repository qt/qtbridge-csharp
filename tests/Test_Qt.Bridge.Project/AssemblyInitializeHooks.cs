// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public static class AssemblyInitializeHooks
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            TempProject.CleanupProjectRoot();
        }
    }
}
