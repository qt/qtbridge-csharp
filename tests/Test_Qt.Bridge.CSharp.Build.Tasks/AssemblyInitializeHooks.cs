// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Test_Qt.Bridge.CSharp.Build.Tasks
{
    [TestClass]
    public static class AssemblyInitializeHooks
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            TestBase.TryDelete(TestBase.RootDirectory);
        }
    }
}
